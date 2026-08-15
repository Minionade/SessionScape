using System;

namespace SessionScape.Main.World
{
    public static class ChunkAuthoring
    {
        public static void MarkEdited(ChunkData chunk)
        {
            chunk.SavedAtUtcTicks = DateTime.UtcNow.Ticks;
            chunk.VersionHash = ChunkHasher.ComputeHash(chunk);
        }
    }
}