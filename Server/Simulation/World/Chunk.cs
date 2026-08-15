using SessionScape.Main.World;

namespace SessionScape.Server.Simulation.World
{
    public class Chunk
    {
        public ChunkData Data { get; }
        public WorldMap Parent { get; }
        public string ContentHash { get; }

        public int LocalX { get; }
        public int LocalZ { get; }

        public Tile[] TileGrid { get; } = new Tile[WorldConstants.ChunkSize * WorldConstants.ChunkSize];

        public Chunk(ChunkData data, WorldMap parent, string contentHash)
        {
            Data = data;
            Parent = parent;
            LocalX = data.X;
            LocalZ = data.Z;
            ContentHash = contentHash;

            Initialize();
        }

        void Initialize()
        {
            for (int tileLocalX = 0; tileLocalX < WorldConstants.ChunkSize; tileLocalX++)
            {
                for (int tileLocalZ = 0; tileLocalZ < WorldConstants.ChunkSize; tileLocalZ++)
                {
                    int index = Data.GetTileIndex(tileLocalX, tileLocalZ);
                    TileCoordinateMath.LocalToWorldTile(LocalX, LocalZ, tileLocalX, tileLocalZ, out int tileWorldX, out int tileWorldZ);

                    TileGrid[index] = new Tile(tileLocalX, tileLocalZ, tileWorldX, tileWorldZ, this);
                }
            }
        }

        public Tile GetTile(int x, int z)
        {
            if (TileGrid == null ||
                x < 0 || x >= WorldConstants.ChunkSize ||
                z < 0 || z >= WorldConstants.ChunkSize)
                return null;

            return TileGrid[Data.GetTileIndex(x, z)];
        }

        public bool TryGetAsset(int instanceId, out AssetInstanceData asset)
        {
            foreach (var a in Data.Assetmap)
            {
                if (a.InstanceId == instanceId)
                {
                    asset = a;
                    return true;
                }
            }

            asset = default;
            return false;
        }
    }
}