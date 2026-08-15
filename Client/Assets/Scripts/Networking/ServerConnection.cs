using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Main.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SessionScape.Client.Assets.Scripts.Networking
{
    public class ServerConnection : MonoBehaviour
    {
        public static event Action<Transform> onPlayerConnected;

        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 7777;
        [SerializeField] private string playerName = "Player";
        [SerializeField] private string password = "dev";
        [SerializeField] private MapLoader mapLoader;

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private int _seq = 0;
        private string _playerId;
        private bool _isRunning;

        private ClientChunkCache _chunkCache;
        private ChunkSyncClient _chunkSyncClient;

        private readonly NetworkEntityManager _entityManager = new();
        private readonly ClientMessageHandlerRegistry _registry = new();

        private readonly Dictionary<(int x, int z), ChunkData> _loadedChunks = new();

        // Debug chat
        private string _chatInput = "";
        private readonly ConcurrentQueue<string> _incomingChatQueue = new();
        private readonly List<string> _chatLog = new();
        private Vector2 _scrollPos;

        // Debug Sprint
        private readonly ConcurrentQueue<double> _runEnergyUpdateQueue = new();
        private float _runEnergy;
        private Texture2D _solidTex;
        private GUIStyle _idleButtonStyle;
        private GUIStyle _runningButtonStyle;

        void Start()
        {
            BuildRegistry();
            Connect();
        }

        void Update()
        {
            if (_stream == null)
                return;

            while (_incomingChatQueue.TryDequeue(out var line))
            {
                _chatLog.Add(line);
            }

            while (_runEnergyUpdateQueue.TryDequeue(out var energy))
            {
                _runEnergy = (float)energy;
            }

            if (_entityManager.PlayerEntity != null)
            {
                _isRunning = _entityManager.PlayerEntity.IsRunning;
            }

            _entityManager.ProcessQueuedActions();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.value);
                if (Physics.Raycast(ray, out var hit, float.MaxValue))
                {
                    int x = Mathf.FloorToInt(hit.point.x);
                    int z = Mathf.FloorToInt(hit.point.z);

                    SendWalkHere(x, z);
                }
            }
        }

        void BuildRegistry()
        {
            _registry.Register(new EchoResponseHandler());
            _registry.Register(new InteractionResponseHandler());
            _registry.Register(new EntityMovementUpdateHandler(_entityManager));
            _registry.Register(new RunUpdateHandler(_entityManager));
            _registry.Register(new RunEnergyUpdateHandler(_runEnergyUpdateQueue));
            _registry.Register(new ChatResponseHandler(_incomingChatQueue));
            _registry.Register(new EntitySnapshotListHandler(_entityManager));
            _registry.Register(new EntityUpdateHandler(_entityManager));
            _registry.Register(new EntityRemovedHandler(_entityManager));

            _entityManager.OnPlayerConnected += t => onPlayerConnected?.Invoke(t);
        }

        void Connect()
        {
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
            Debug.Log("Connected to Server");

            if (!Login())
            {
                Debug.LogError("Login was rejected by server. Disconnecting.");
                _stream.Close();
                _client.Close();
                return;
            }

            _entityManager.PlayerId = _playerId;

            // NOTE: like Login(), this runs synchronously on the main thread
            // (Connect() is called from Start()). That's consistent with the
            // existing Login() pattern, but a full/partial chunk sync can take
            // noticeably longer than a login round trip -- worth moving both
            // onto a background thread (or async) later if a loading freeze
            // becomes noticeable.
            if (!PerformChunkSync())
            {
                Debug.LogError("Chunk sync failed. Disconnecting.");
                _stream.Close();
                _client.Close();
                return;
            }

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
        }

        bool Login()
        {
            var loginRequest = new LoginRequest()
            {
                PlayerName = playerName,
                Password = password
            };

            var envelope = MessageEnvelope.Create(MessageType.LoginRequest, _seq++, 0, loginRequest);
            MessageFramer.WriteMessage(_stream, envelope);

            MessageEnvelope response = MessageFramer.ReadMessage(_stream);

            if (response == null || response.Type != MessageType.LoginResponse)
            {
                Debug.LogError("Did not recieve a valid LoginResponse");
                return false;
            }

            var data = response.GetData<LoginResponse>();

            if (!data.Accepted)
            {
                Debug.LogError(data.RejectReason);
                return false;
            }

            _playerId = data.PlayerId;
            Debug.Log("Login Accepted.  Player Id: " + _playerId);
            return true;
        }

        bool PerformChunkSync()
        {
            try
            {
                _chunkCache = new ClientChunkCache(Path.Combine(Application.persistentDataPath, "MapCache"));
                _chunkSyncClient = new ChunkSyncClient(_stream, _chunkCache);

                bool wasUpToDate = _chunkSyncClient.PerformLoginSync(chunk => _loadedChunks[(chunk.X, chunk.Z)] = chunk,
                    (x, z) => { _loadedChunks.Remove((x, z)); mapLoader.UnloadChunk(x, z); });

                int loadedFromDisk = 0;

                foreach ((int x, int z) in _chunkCache.ListCachedChunkCoordinates())
                {
                    if (_loadedChunks.ContainsKey((x, z)))
                        continue; // already have it fresh from the network this session

                    ChunkData cached = _chunkCache.LoadChunk(x, z);

                    if (cached != null)
                    {
                        _loadedChunks[(x, z)] = cached;
                        loadedFromDisk++;
                    }
                }

                Debug.Log($"Chunk sync complete ({(wasUpToDate ? "fully up to date" : "partial/full download")}). " +
                          $"{_loadedChunks.Count} chunks total, {loadedFromDisk} loaded from local cache.");

                mapLoader.LoadChunks(_loadedChunks.Values);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Chunk sync failed: " + ex.Message);
                return false;
            }
        }

        void SendWalkHere(int x, int z)
        {
            var envelope = MessageEnvelope.Create(MessageType.InteractionRequest, _seq++, 0,
                new InteractionRequest
                {
                    TargetType = InteractionTargetType.Tile,
                    Verb = InteractionVerb.WalkHere,
                    TargetX = x,
                    TargetZ = z
                });

            MessageFramer.WriteMessage(_stream, envelope);
        }

        void SendRunRequest(bool isRunning)
        {
            _isRunning = isRunning;

            var envelope = MessageEnvelope.Create(MessageType.RunRequest, _seq++, 0,
                new RunRequest() { IsSprinting = isRunning });

            MessageFramer.WriteMessage(_stream, envelope);
        }

        void SendChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var envelope = MessageEnvelope.Create(MessageType.ChatRequest, _seq++, 0,
                new ChatRequest() { Text = text });

            MessageFramer.WriteMessage(_stream, envelope);
        }

        void ReceiveLoop()
        {
            while (true)
            {
                MessageEnvelope envelope = MessageFramer.ReadMessage(_stream);
                if (envelope == null)
                    break;

                if (!_registry.TryHandle(envelope))
                {
                    Debug.LogWarning($"No client handler registered for message type: {envelope.Type}");
                }
            }
        }

        void OnGUI()
        {
            if (_solidTex == null)
            {
                _solidTex = new Texture2D(1, 1);
                _solidTex.SetPixel(0, 0, Color.white);
                _solidTex.Apply();
            }

            if (_idleButtonStyle == null)
            {
                _idleButtonStyle = new GUIStyle(GUI.skin.button);
                Texture2D greyTex = new Texture2D(1, 1);
                greyTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f)); // transparent grey
                greyTex.Apply();
                _idleButtonStyle.normal.background = greyTex;
                _idleButtonStyle.hover.background = greyTex;
                _idleButtonStyle.active.background = greyTex;
            }

            if (_runningButtonStyle == null)
            {
                _runningButtonStyle = new GUIStyle(GUI.skin.button);
                Texture2D barelyVisibleTex = new Texture2D(1, 1);
                barelyVisibleTex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.05f)); // near-invisible
                barelyVisibleTex.Apply();
                _runningButtonStyle.normal.background = barelyVisibleTex;
                _runningButtonStyle.hover.background = barelyVisibleTex;
                _runningButtonStyle.active.background = barelyVisibleTex;
            }

            float buttonSize = 70f;
            float padding = 15f;

            Rect runButtonRect = new Rect(
                Screen.width - buttonSize - padding,
                padding,
                buttonSize,
                buttonSize);

            float energyT = Mathf.Clamp01(_runEnergy / 100f);

            Color emptyColor = new Color(0.5f, 0.5f, 0.5f);
            Color fullColor = new Color(1f, 0.92f, 0f);

            GUI.color = emptyColor;
            GUI.DrawTexture(runButtonRect, _solidTex);

            float fillHeight = runButtonRect.height * energyT;
            Rect fillRect = new Rect(
                runButtonRect.x,
                runButtonRect.y + (runButtonRect.height - fillHeight),
                runButtonRect.width,
                fillHeight);

            GUI.color = fullColor;
            GUI.DrawTexture(fillRect, _solidTex);

            GUI.color = Color.white;
            GUIStyle activeStyle = _isRunning ? _runningButtonStyle : _idleButtonStyle;

            if (GUI.Button(runButtonRect, "RUN", activeStyle))
            {
                SendRunRequest(!_isRunning);
            }

            float boxWidth = 300f;
            float boxHeight = 200f;

            GUILayout.BeginArea(new Rect(padding, Screen.height - boxHeight - padding, boxWidth, boxHeight), GUI.skin.box);

            GUILayout.Label("Chat");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(boxHeight - 60f));
            foreach (var line in _chatLog)
            {
                GUILayout.Label(line);
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

            GUI.SetNextControlName("ChatInput");
            _chatInput = GUILayout.TextField(_chatInput, GUILayout.ExpandWidth(true));

            bool pressedEnter = Event.current.isKey &&
                                Event.current.keyCode == KeyCode.Return &&
                                GUI.GetNameOfFocusedControl() == "ChatInput";

            if (GUILayout.Button("Send", GUILayout.Width(50f)) || pressedEnter)
            {
                SendChat(_chatInput);
                _chatInput = "";
                GUI.FocusControl("ChatInput");
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void OnApplicationQuit()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}