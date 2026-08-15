using SessionScape.Main.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SessionScape.Server.Simulation.World
{
    public class WorldMap
    {
        private readonly ConcurrentDictionary<(int x, int z), Chunk> _chunks = new();
        private string _cachedMapHash;

        public WorldMap()
        { }

        public void RegisterChunk(int x, int z, Chunk chunk)
        {
            _chunks[(x, z)] = chunk;
            _cachedMapHash = null;
        }

        public void UnregisterChunk(int x, int z)
        {
            _chunks.Remove((x, z), out _);
            _cachedMapHash = null;
        }

        public IEnumerable<Chunk> GetAllChunks() => _chunks.Values;

        public string ComputeMapHash()
        {
            if (_cachedMapHash == null)
            {
                List<ChunkManifest> manifestEntries = new();

                foreach (Chunk chunk in _chunks.Values)
                {
                    manifestEntries.Add(new ChunkManifest
                    {
                        ChunkX = chunk.LocalX,
                        ChunkZ = chunk.LocalZ,
                        ContentHash = chunk.ContentHash
                    });
                }

                _cachedMapHash = MapManifestSerializer.ComputeMapHash(manifestEntries);
            }

            return _cachedMapHash;
        }

        private bool TryGetNeighbor(Tile tile, int searchX, int searchZ, out Tile neighbor)
        {
            neighbor = null;

            int worldX = tile.WorldX + searchX;
            int worldZ = tile.WorldZ + searchZ;

            if (!TryGetChunkFromWorld(worldX, worldZ, out Chunk neighborChunk))
                return false;

            TileCoordinateMath.WorldToLocalTile(worldX, worldZ, out int localX, out int localZ);

            neighbor = neighborChunk.GetTile(localX, localZ);

            if (neighbor == null)
                return false;

            return true;
        }

        private bool CanMoveBetween(Tile from, Tile to, int offsetX, int offsetZ)
        {
            if (!from.Walkable || !to.Walkable)
                return false;

            if (Math.Abs(offsetX) + (Math.Abs(offsetZ)) == 1)
            {
                TileConnections direction = TileCoordinateMath.GetDirection(offsetX, offsetZ);
                TileConnections opposite = TileCoordinateMath.GetOpposite(direction);

                return from.Connections.HasFlag(direction) && to.Connections.HasFlag(opposite);
            }

            return CanMoveDiagonally(from, to, offsetX, offsetZ);
        }

        private bool CanMoveDiagonally(Tile from, Tile to, int offsetX, int offsetZ)
        {
            TileConnections horizontal = offsetX > 0 ? TileConnections.east : TileConnections.west;
            TileConnections vertical = offsetZ > 0 ? TileConnections.north : TileConnections.south;

            if (!from.Connections.HasFlag(horizontal))
                return false;
            if (!to.Connections.HasFlag(TileCoordinateMath.GetOpposite(horizontal)))
                return false;

            if (!from.Connections.HasFlag(vertical))
                return false;
            if (!to.Connections.HasFlag(TileCoordinateMath.GetOpposite(vertical)))
                return false;

            if (!TryGetNeighbor(from, offsetX, 0, out Tile horizontalTile))
                return false;
            if (!TryGetNeighbor(from, 0, offsetZ, out Tile verticalTile))
                return false;

            return horizontalTile.Walkable && verticalTile.Walkable;
        }

        public bool TryGetChunkFromLocal(int chunkX, int chunkZ, out Chunk chunk)
        {
            chunk = null;
            if (!_chunks.ContainsKey((chunkX, chunkZ)))
                return false;

            chunk = _chunks[(chunkX, chunkZ)];
            return true;
        }
        
        public bool TryGetChunkFromWorld(int worldX, int worldZ, out Chunk chunk)
        {
            TileCoordinateMath.WorldToChunkCoord(worldX, worldZ, out int chunkX, out int chunkZ);
            return TryGetChunkFromLocal(chunkX, chunkZ, out chunk);
        }

        public bool TryGetTile(int worldX, int worldZ, out Tile tile)
        {
            tile = null;
            if (!TryGetChunkFromWorld(worldX, worldZ, out Chunk chunk))
                return false;

            TileCoordinateMath.WorldToLocalTile(worldX, worldZ, out int localX, out int localZ);
            tile = chunk.GetTile(localX, localZ);

            return tile != null;
        }

        public Tile GetClosestValidTile(Tile targetTile, int anchorX, int anchorZ)
        {
            if (targetTile == null)
                return null;

            if (targetTile.Walkable)
                return targetTile;

            List<Tile> neighbors = GetTileNeighbors(targetTile, onlyValidTiles: true);

            if (neighbors.Count > 0)
            {
                Tile finalTile = null;
                double distance = GetSquareDistance(targetTile.LocalX, targetTile.LocalZ, anchorX, anchorZ);
                foreach (Tile neighborTile in neighbors)
                {
                    double neighborDistance = GetSquareDistance(neighborTile.LocalX, neighborTile.LocalZ, anchorX, anchorZ);

                    if (finalTile == null || neighborDistance < distance)
                        finalTile = neighborTile;
                }

                if (finalTile != null)
                    return finalTile;
            }

            return SearchForClosestValidTile(targetTile);
        }

        public List<Tile> GetTileNeighbors(Tile targetTile, bool onlyValidTiles = false)
        {
            List<Tile> neighbors = new();

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0)
                        continue;

                    if (!TryGetNeighbor(targetTile, x, z, out Tile neighborTile))
                        continue;

                    if (onlyValidTiles && !CanMoveBetween(targetTile, neighborTile, x, z))
                        continue;

                    neighbors.Add(neighborTile);
                }
            }

            return neighbors;
        }


        private Tile SearchForClosestValidTile(Tile targetTile)
        {
            const int MAX_SEARCH_INTERVAL = 64;

            Queue<(Tile tile, int distance)> openSet = new();
            HashSet<Tile> closedSet = new();

            openSet.Enqueue((targetTile, 0));
            closedSet.Add(targetTile);

            while (openSet.Count > 0)
            {
                var (currentTile, distance) = openSet.Dequeue();

                if (distance >= MAX_SEARCH_INTERVAL)
                    continue;

                List<Tile> neighbors = GetTileNeighbors(currentTile);

                if (neighbors == null || neighbors.Count == 0)
                    continue;

                foreach (Tile neighborTile in neighbors)
                {
                    if (neighborTile == null || closedSet.Contains(neighborTile))
                        continue;

                    closedSet.Add(neighborTile);

                    if (neighborTile.Walkable)
                        return neighborTile;

                    openSet.Enqueue((neighborTile, distance + 1));
                }
            }

            return null;
        }

        private double GetSquareDistance(int startX, int startZ, int endX, int endZ)
        {
            double distanceX = (startX - endX);
            double distanceZ = (startZ - endZ);
            return Math.Sqrt((distanceX * distanceX) + (distanceZ * distanceZ));
        }
    }
}
