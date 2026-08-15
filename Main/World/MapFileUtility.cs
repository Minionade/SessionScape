using System.IO;

namespace SessionScape.Main.World
{
    public static class MapFileUtility
    {
        public static string GetChunkFileName(int chunkX, int chunkZ)
        {
            return chunkX + "_" + chunkZ + ".bin";
        }

        public static string GetChunkPath(string mapDirectory, int chunkX, int chunkZ)
        {
            string chunksDirectory = Path.Combine(mapDirectory, "chunks");

            return Path.Combine(chunksDirectory, GetChunkFileName(chunkX, chunkZ));
        }
    }
}