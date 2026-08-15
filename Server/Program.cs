using Server.Simulation;
using Server.Simulation.World;
using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Server.Simulation;
using SessionScape.Server.Simulation.Handlers;
using SessionScape.Server.Simulation.Handlers.Interaction;
using SessionScape.Server.Simulation.Handlers.Interactions;
using SessionScape.Server.Simulation.World;
using System.Net;
using System.Net.Sockets;

var playerAccountRepository = new PlayerAccountRepository("E:\\Game Projects\\SessionScape\\Server\\Data\\Accounts");

var world = new WorldState();
var worldMap = new WorldMap();
var mapLoader = new MapLoader("E:\\Game Projects\\SessionScape\\Client\\Assets\\Maps\\DefaultMap");
mapLoader.LoadMap(worldMap);
var chunkStreamer = new ChunkStreamer(worldMap);

var pathRequestQueue = new PathRequestQueue(worldMap);
var movementSystem = new MovementSystem(world);

var mobSpawner = new MobSpawner(world);

var random = new Random();
for (int i = 0; i < 101; i++)
{
    int x = random.Next(-32, 33);
    int z = random.Next(-32, 33);
    mobSpawner.RegisterSpawnPoint(EntityType.Npc, x, z);
}

mobSpawner.SpawnAll();

var mobAiSystem = new MobAiSystem(world, pathRequestQueue);

var resolver = new InteractionResolver();
resolver.Register(new WalkHereHandler(world, chunkStreamer, pathRequestQueue));

var registry = new MessageHandlerRegistry();
registry.Register(new InteractionHandler(resolver));
registry.Register(new EchoHandler());
registry.Register(new SprintHandler(world));
registry.Register(new ChatHandler(world));

var tickLoop = new TickLoop(registry);
tickLoop.Start();

var listener = new TcpListener(IPAddress.Any, 7777);
listener.Start();
Console.WriteLine("Server listening on port 7777...");

while (true)
{
    TcpClient client = listener.AcceptTcpClient();
    Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);
    _ = Task.Run(() => HandleClient(client));
}

void HandleClient(TcpClient client)
{
    using NetworkStream stream = client.GetStream();

    stream.WriteTimeout = 5000;

    var player = PerformLoginHandshake(stream);
    if (player == null)
    {
        client.Close();
        return;
    }

    var connectedPlayer = new ConnectedPlayer(player, stream);

    if (!chunkStreamer.PerformLoginSync(connectedPlayer))
    {
        client.Close();
        return;
    }

    var existingSnapshots = world.Entities.Values
        .Select(EntitySnapshotHelper.AsSnapshot)
        .ToList();

    world.AddPlayer(connectedPlayer);

    var snapshotEnvelope = MessageEnvelope.Create(MessageType.EntitySnapshotList, 0, 0,
        new EntitySnapshotList { Entities = existingSnapshots });
    MessageFramer.WriteMessage(stream, snapshotEnvelope);

    if (worldMap.TryGetTile(player.X, player.Z, out Tile playerTile))
    {
        player.Y = playerTile.WorldY;
    }
    else
    {
        worldMap.TryGetTile(0, 0, out playerTile);
        player.X = playerTile.WorldX;
        player.Y = playerTile.WorldY;
        player.Z = playerTile.WorldZ;
    }

    var joinUpdate = MessageEnvelope.Create(MessageType.EntityUpdate, 0, 0,
        new EntityUpdate { Entity = EntitySnapshotHelper.AsSnapshot(player) });
    world.BroadcastToAll(joinUpdate);

    try
    {
        while (true)
        {
            MessageEnvelope envelope = MessageFramer.ReadMessage(stream);
            if (envelope == null)
                break;

            tickLoop.Enqueue(new PendingAction(envelope, stream, player));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Client error:" + ex.Message);
    }
    finally
    {
        TickLoop.OnTick -= player.Movement.RestoreRunEnergy;

        player.Account.X = player.X;
        player.Account.Y = player.Y;
        player.Account.Z = player.Z;
        player.Account.RunEnergy = player.Movement.RunEnergy;

        playerAccountRepository.Save(player.Account);

        world.RemovePlayer(player.Id);

        var removedUpdate = MessageEnvelope.Create(MessageType.EntityRemoved, 0, 0,
            new EntityRemoved { EntityId = player.Id.ToString() });

        world.BroadcastToAll(removedUpdate);

        client.Close();
        Console.WriteLine("Client disconnected.");
    }
}

PlayerState PerformLoginHandshake(NetworkStream stream)
{
    MessageEnvelope envelope = MessageFramer.ReadMessage(stream);
    if (envelope == null || envelope.Type != MessageType.LoginRequest)
    {
        Console.WriteLine("Client did not send Login Request, dropping connection.");
        return null;
    }

    var request = envelope.GetData<LoginRequest>();

    if (!playerAccountRepository.TryLoad(request.PlayerName, out PlayerAccount account))
    {
        account = playerAccountRepository.CreateAccount(request.PlayerName, request.Password);

        //Console.WriteLine($"Login rejected: Password or Username is invalid.");
        //var rejection = MessageEnvelope.Create(MessageType.LoginResponse, envelope.Sequence, 0,
        //    new LoginResponse { Accepted = false, RejectReason = $"Login rejected: Password or Username is invalid." });
    }

    if (!playerAccountRepository.VerifyPassword(account, request.Password))
    {
        Console.WriteLine($"Login rejected: Password or username is invalid.");
        var rejection = MessageEnvelope.Create(MessageType.LoginResponse, envelope.Sequence, 0,
            new LoginResponse { Accepted = false, RejectReason = $"Login rejected: Password or Username is invalid." });

        return null;
    }

    var player = new PlayerState
    {
        Name = request.PlayerName,
        X = account.X,
        Y = account.Y,
        Z = account.Z,
        Account = account
    };


    var accepted = MessageEnvelope.Create(MessageType.LoginResponse, envelope.Sequence, 0,
        new LoginResponse { Accepted = true, PlayerId = player.Id.ToString() });

    MessageFramer.WriteMessage(stream, accepted);

    player.Movement.SetRunEnergy(account.RunEnergy);
    TickLoop.OnTick += player.Movement.RestoreRunEnergy;

    Console.WriteLine($"Login accepted: {player.Name} ({player.Id})");
    return player;
}