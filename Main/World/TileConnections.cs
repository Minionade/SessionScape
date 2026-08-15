namespace SessionScape.Main.World
{
    [System.Flags]
    public enum TileConnections
    {
        none = 0,
        north = 1 << 0,
        south = 1 << 1,
        east = 1 << 2,
        west = 1 << 3,
    }
}