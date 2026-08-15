using SessionScape.Main.World;

namespace SessionScape.Server.Simulation.World
{
    public class Tile : IHeapItem<Tile>
    {
        public int LocalX { get; }
        public int LocalZ { get; }
        public int WorldX { get; }
        public float WorldY
        {
            get { return Chunk.Data.GetTileHeight(LocalX, LocalZ); }
        }
        public int WorldZ { get; }

        public Chunk Chunk { get; }

        public bool Walkable
        {
            get { return Connections != TileConnections.none && Chunk.Data.IsTileWalkable(LocalX, LocalZ); }
        }

        public TileConnections Connections
        {
            get { return Chunk.Data.GetTileConnections(LocalX, LocalZ); }
        }

        public int HeapIndex { get; set; }
        public Tile? Parent { get; set; }
        public (int x, int z) EnterDirection { get; set; }
        public int GCost { get; set; }
        public int HCost { get; set; }
        public int FCost
        {
            get { return GCost + HCost; }
        }

        public Tile(int localX, int localZ, int globalX, int globalZ, Chunk chunk)
        {
            LocalX = localX;
            LocalZ = localZ;
            WorldX = globalX;
            WorldZ = globalZ;
            Chunk = chunk;
        }

        public int CompareTo(Tile? other)
        {
            if (other == null)
                return 1;

            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
                compare = HCost.CompareTo(other.HCost);

            return -compare;
        }
    }
}