using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Main.World;
using System;
using System.Net.Sockets;

namespace SessionScape.Client.Assets.Scripts.Networking
{
    public class ChunkSyncClient
    {
        private readonly NetworkStream _stream;
        private readonly ClientChunkCache _cache;

        public ChunkSyncClient(NetworkStream stream, ClientChunkCache cache)
        {
            _stream = stream;
            _cache = cache;
        }

        public bool PerformLoginSync(Action<ChunkData> onChunkReceived, Action<int, int> onChunkRemoved)
        {
            string cachedMapHash = _cache.LoadCachedMapHash();
            var cachedChunks = _cache.GetCachedChunkManifest();

            var requestEnvelope = MessageEnvelope.Create(MessageType.MapSyncRequest, 0, 0, 
                new MapSyncRequest { CachedMapHash = cachedMapHash, CachedChunks = cachedChunks });

            MessageFramer.WriteMessage(_stream, requestEnvelope);

            while (true)
            {
                MessageEnvelope envelope = MessageFramer.ReadMessage(_stream);

                if (envelope == null)
                    throw new InvalidOperationException("Connection closed during map sync.");

                switch (envelope.Type)
                {
                    case MessageType.MapUpToDate:
                        Console.WriteLine("[ChunkSyncClient] Map is up to date, using cached chunks.");
                        return true;

                    case MessageType.ChunkSnapshot:
                        ChunkSnapshot snapshot = envelope.GetData<ChunkSnapshot>();
                        _cache.SaveChunk(snapshot.Chunk, snapshot.ContentHash);
                        onChunkReceived(snapshot.Chunk);
                        break;

                    case MessageType.MapSyncComplete:
                        MapSyncComplete complete = envelope.GetData<MapSyncComplete>();
                        _cache.SaveMapHash(complete.MapHash);
                        Console.WriteLine($"[ChunkSyncClient] Server says {complete.RemovedChunks?.Count ?? 0} chunks were removed.");
                        foreach (ChunkCoordinate coord in complete.RemovedChunks ?? new())
                        {
                            _cache.DeleteChunk(coord.X, coord.Z);
                            onChunkRemoved(coord.X, coord.Z);
                        }

                        _cache.FlushChunkIndex();
                        return false;

                    default:
                        Console.WriteLine($"[ChunkSyncClient] Unexpected message {envelope.Type} during map sync, ignoring.");
                        break;
                }
            }
        }
    }
}