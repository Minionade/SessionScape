namespace SessionScape.Main.World
{
    public static class TileCoordinateMath
    {
        public static TileConnections GetDirection(int offsetX, int offsetZ)
        {
            if (offsetX > 0)
                return TileConnections.east;
            if (offsetX < 0)
                return TileConnections.west;
            if (offsetZ > 0)
                return TileConnections.north;
            if (offsetZ < 0)
                return TileConnections.south;

            return TileConnections.none;
        }

        public static TileConnections GetOpposite(TileConnections direction)
        {
            return direction switch
            {
                TileConnections.north => TileConnections.south,
                TileConnections.south => TileConnections.north,
                TileConnections.east => TileConnections.west,
                TileConnections.west => TileConnections.east,
                _ => TileConnections.none
            };
        }

        public static void WorldToChunkCoord(int worldX, int worldZ, out int chunkX, out int chunkZ)
        {
            int halfChunkSize = WorldConstants.ChunkSize / 2;

            chunkX = FloorDiv(worldX + halfChunkSize, WorldConstants.ChunkSize);
            chunkZ = FloorDiv(worldZ + halfChunkSize, WorldConstants.ChunkSize);
        }

        public static void WorldToLocalTile(int worldX, int worldZ, out int localX, out int localZ)
        {
            int halfChunkSize = WorldConstants.ChunkSize / 2;

            localX = Mod(worldX + halfChunkSize, WorldConstants.ChunkSize);
            localZ = Mod(worldZ + halfChunkSize, WorldConstants.ChunkSize);
        }

        public static void LocalToWorldTile(int chunkX, int chunkZ, int localX, int localZ, out int worldX, out int worldZ)
        {
            int halfChunkSize = WorldConstants.ChunkSize / 2;

            worldX = chunkX * WorldConstants.ChunkSize - halfChunkSize + localX;
            worldZ = chunkZ * WorldConstants.ChunkSize - halfChunkSize + localZ;
        }

        public static void WorldToLocalVertex(int worldX, int worldZ, out int localX, out int localZ)
        {
            int vertexCount = WorldConstants.ChunkSize + 1;

            localX = Mod(worldX, vertexCount);
            localZ = Mod(worldZ, vertexCount);
        }

        public static void LocalToWorldVertex(int chunkX, int chunkZ, int localX, int localZ, out int worldX, out int worldZ)
        {
            int vertexCount = WorldConstants.ChunkSize + 1;

            worldX = chunkX * vertexCount + localX;
            worldZ = chunkZ * vertexCount + localZ;
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            if (a % b != 0 && (a < 0) != (b < 0))
                q--;

            return q;
        }

        private static int Mod(int a, int b)
        {
            int m = a % b;
            return m < 0 ? m + b : m;
        }
    }
}