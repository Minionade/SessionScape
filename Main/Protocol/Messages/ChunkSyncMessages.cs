using SessionScape.Main.World;
using System.Collections.Generic;

namespace SessionScape.Main.Protocol
{
    public class ChunkSnapshot
    {
        public ChunkData Chunk;
        public string ContentHash;
    }

    public class MapSyncRequest
    {
        public string CachedMapHash;
        public List<ChunkManifest> CachedChunks;
    }

    public class ChunkCoordinate { public int X; public int Z; }

    public class MapSyncComplete
    {
        public string MapHash;
        public int ChunkCount;
        public List<ChunkCoordinate> RemovedChunks;
    }

    public class MapUpToDate 
    { }
}