using System;

namespace SessionScape.Main.World
{
    [Serializable]
    public class ChunkData
    {
        public int X, Z;
        public TileData[] Tilemap;
        public VertexData[] Vertexmap;
        public AssetInstanceData[] Assetmap;
        public long SavedAtUtcTicks;
        public string VersionHash = string.Empty;

        public static ChunkData CreateEmpty(int chunkX, int chunkZ, int chunkSize)
        {
            var tiles = new TileData[chunkSize * chunkSize];

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    tiles[z * chunkSize + x] = new TileData { Walkable = true, Connections = TileConnections.north | TileConnections.south | TileConnections.east | TileConnections.west };
                }
            }

            int vertsPerRow = chunkSize + 1;
            var vertices = new VertexData[vertsPerRow * vertsPerRow];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new VertexData { Height = 0, R = 255, G = 255, B = 255, A = 255 };
            }

            return new ChunkData { X = chunkX, Z = chunkZ, Tilemap = tiles, Vertexmap = vertices };
        }

        public int GetTileIndex(int localX, int localZ)
        {
            if (localX < 0 || localX >= WorldConstants.ChunkSize || localZ < 0 || localZ >= WorldConstants.ChunkSize)
                return -1;

            return localZ * WorldConstants.ChunkSize + localX;
        }

        public bool IsTileWalkable(int localX, int localZ)
        {
            int index = GetTileIndex(localX, localZ);
            if (index < 0)
                return false;

            return Tilemap[index].Walkable;
        }

        public TileConnections GetTileConnections(int localX, int localZ)
        {
            int index = GetTileIndex(localX, localZ);

            if (index < 0 || index >= Tilemap.Length)
                return TileConnections.none;

            return Tilemap[index].Connections;
        }

        public float GetTileHeight(int localX, int localZ)
        {
            float average = 0;
            average += GetVertexHeight(localX, localZ);
            average += GetVertexHeight(localX + 1, localZ);
            average += GetVertexHeight(localX, localZ + 1);
            average += GetVertexHeight(localX + 1, localZ + 1);

            return average / 4f;
        }

        public int GetVertexIndex(int localX, int localZ)
        {
            int vertsPerRow = WorldConstants.ChunkSize + 1;
            if (localX < 0 || localX >= vertsPerRow || localZ < 0 || localZ >= vertsPerRow)
                return -1;

            return localZ * vertsPerRow + localX;
        }

        public float GetVertexHeight(int localX, int localZ)
        {
            int index = GetVertexIndex(localX, localZ);
            if (index < 0)
                return 0f;

            return Vertexmap[index].Height;
        }

        public void GetVertexColor(int localX, int localZ, out (byte R, byte G, byte B, byte A) color)
        {
            color.R = color.G = color.B = color.A = 255;
            int index = GetVertexIndex(localX, localZ);
            if (index < 0)
                return;
            var data = Vertexmap[index];

            color = (data.R, data.G, data.B, data.A); 
        }
    }
}