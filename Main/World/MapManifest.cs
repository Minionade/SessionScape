using System.Collections.Generic;

namespace SessionScape.Main.World
{
    [System.Serializable]
    public class MapManifest
    {
        public string MapId;
        public int FormatVersion;
        public int MapVersion;

        public string ContentHash;

        public List<ChunkManifest> Chunks;
    }
}