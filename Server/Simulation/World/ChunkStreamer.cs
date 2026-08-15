using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Main.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SessionScape.Server.Simulation.World
{
    public class ChunkStreamer
    {
        private readonly WorldMap _worldMap;

        public ChunkStreamer(WorldMap worldMap)
        {
            _worldMap = worldMap;
        }

        public bool PerformLoginSync(ConnectedPlayer player)
        {
            MessageEnvelope requestEnvelope = MessageFramer.ReadMessage(player.Stream);

            if (requestEnvelope == null || requestEnvelope.Type != MessageType.MapSyncRequest)
            {
                Console.WriteLine(
                    "[ChunkStreamer] Client did not send a MapSyncRequest during login sync, dropping connection.");

                return false;
            }

            MapSyncRequest request = requestEnvelope.GetData<MapSyncRequest>();

            string currentMapHash = _worldMap.ComputeMapHash();

            Dictionary<(int x, int z), string> clientChunkHashes = new();

            if (request?.CachedChunks != null)
            {
                foreach (ChunkManifest entry in request.CachedChunks)
                {
                    clientChunkHashes[(entry.ChunkX, entry.ChunkZ)] = entry.ContentHash;
                }
            }

            List<Chunk> serverChunks = _worldMap.GetAllChunks().ToList();

            Dictionary<(int x, int z), string> serverChunkHashes = serverChunks
                .ToDictionary(
                    chunk => (chunk.LocalX, chunk.LocalZ),
                    chunk => chunk.ContentHash);

            bool mapHashMatches =
                !string.IsNullOrEmpty(request?.CachedMapHash) &&
                string.Equals(
                    request.CachedMapHash,
                    currentMapHash,
                    StringComparison.OrdinalIgnoreCase);

            bool chunkManifestMatches =
                clientChunkHashes.Count == serverChunkHashes.Count &&
                clientChunkHashes.All(pair =>
                    serverChunkHashes.TryGetValue(pair.Key, out string serverHash) &&
                    string.Equals(
                        pair.Value,
                        serverHash,
                        StringComparison.OrdinalIgnoreCase));

            if (mapHashMatches && chunkManifestMatches)
            {
                Console.WriteLine(
                    $"[ChunkStreamer] {player.State.Name}'s map and chunk cache match. " +
                    $"Skipping chunk stream. {serverChunkHashes.Count} chunks verified.");

                var upToDateEnvelope = MessageEnvelope.Create(
                    MessageType.MapUpToDate,
                    0,
                    0,
                    new MapUpToDate());

                MessageFramer.WriteMessage(player.Stream, upToDateEnvelope);

                return true;
            }

            Console.WriteLine(
                $"[ChunkStreamer] {player.State.Name}'s cache requires synchronization. " +
                $"MapHashMatch={mapHashMatches}, " +
                $"ClientChunks={clientChunkHashes.Count}, " +
                $"ServerChunks={serverChunkHashes.Count}.");

            int sentCount = 0;

            foreach (Chunk chunk in serverChunks)
            {
                if (clientChunkHashes.TryGetValue(
                        (chunk.LocalX, chunk.LocalZ),
                        out string clientHash)
                    && string.Equals(
                        clientHash,
                        chunk.ContentHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SendChunk(player, chunk);
                sentCount++;
            }

            HashSet<(int x, int z)> serverCoords =
                serverChunkHashes.Keys.ToHashSet();

            List<ChunkCoordinate> removed = clientChunkHashes.Keys
                .Where(coord => !serverCoords.Contains(coord))
                .Select(coord => new ChunkCoordinate
                {
                    X = coord.x,
                    Z = coord.z
                })
                .ToList();

            Console.WriteLine(
                $"[ChunkStreamer] Client has {clientChunkHashes.Count} cached chunks, " +
                $"server has {serverChunkHashes.Count} chunks. " +
                $"Sending {sentCount} chunks and removing {removed.Count} stale chunks.");

            var completeEnvelope = MessageEnvelope.Create(
                MessageType.MapSyncComplete,
                0,
                0,
                new MapSyncComplete
                {
                    MapHash = currentMapHash,
                    ChunkCount = serverChunks.Count,
                    RemovedChunks = removed
                });

            MessageFramer.WriteMessage(player.Stream, completeEnvelope);

            Console.WriteLine(
                $"[ChunkStreamer] Sync complete for {player.State.Name}. " +
                $"Sent {sentCount} changed/missing chunks. " +
                $"Removed {removed.Count} stale chunks.");

            return true;
        }

        private void SendChunk(ConnectedPlayer player, Chunk chunk)
        {
            ChunkSnapshot snapshot = new ChunkSnapshot
            {
                Chunk = chunk.Data,
                ContentHash = chunk.ContentHash
            };

            var envelope = MessageEnvelope.Create(
                MessageType.ChunkSnapshot,
                0,
                0,
                snapshot);

            MessageFramer.WriteMessage(player.Stream, envelope);

            player.LoadedChunks[(chunk.LocalX, chunk.LocalZ)] = chunk.ContentHash;
        }
    }
}