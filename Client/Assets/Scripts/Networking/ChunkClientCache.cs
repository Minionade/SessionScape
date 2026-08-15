using SessionScape.Main.World;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SessionScape.Client.Assets.Scripts.Networking
{
    public class ClientChunkCache
    {
        private readonly string _cacheDirectory;
        private readonly string _mapHashPath;
        private readonly string _chunkIndexPath;
        private readonly Dictionary<(int x, int z), string> _chunkIndex = new();

        public ClientChunkCache(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
            _mapHashPath = Path.Combine(_cacheDirectory, "map_hash.txt");
            _chunkIndexPath = Path.Combine(_cacheDirectory, "chunk_index.bin");

            Directory.CreateDirectory(Path.Combine(_cacheDirectory, "chunks"));

            LoadChunkIndex();
        }

        public List<(int x, int z)> ListCachedChunkCoordinates() => _chunkIndex.Keys.ToList();

        public string LoadCachedMapHash()
        {
            return File.Exists(_mapHashPath) ? File.ReadAllText(_mapHashPath).Trim() : null;
        }

        public void SaveMapHash(string mapHash)
        {
            File.WriteAllText(_mapHashPath, mapHash);
        }

        public List<ChunkManifest> GetCachedChunkManifest()
        {
            List<ChunkManifest> result = new();

            foreach (var kvp in _chunkIndex)
            {
                result.Add(new ChunkManifest
                {
                    ChunkX = kvp.Key.x,
                    ChunkZ = kvp.Key.z,
                    ContentHash = kvp.Value
                });
            }

            return result;
        }

        public ChunkData LoadChunk(int x, int z)
        {
            string path = MapFileUtility.GetChunkPath(_cacheDirectory, x, z);

            if (!File.Exists(path))
                return null;

            return ChunkBinarySerializer.Deserialize(File.ReadAllBytes(path));
        }

        public void SaveChunk(ChunkData chunk, string contentHash)
        {
            string path = MapFileUtility.GetChunkPath(_cacheDirectory, chunk.X, chunk.Z);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllBytes(path, ChunkBinarySerializer.Serialize(chunk));

            _chunkIndex[(chunk.X, chunk.Z)] = contentHash;
        }

        public void DeleteChunk(int x, int z)
        {
            string path = MapFileUtility.GetChunkPath(_cacheDirectory, x, z);
            if (File.Exists(path))
                File.Delete(path);

            _chunkIndex.Remove((x, z));
        }

        public void FlushChunkIndex()
        {
            using FileStream stream = File.Create(_chunkIndexPath);
            using BinaryWriter writer = new(stream);

            writer.Write(_chunkIndex.Count);

            foreach (var kvp in _chunkIndex)
            {
                writer.Write(kvp.Key.x);
                writer.Write(kvp.Key.z);
                writer.Write(kvp.Value);
            }
        }

        private void LoadChunkIndex()
        {
            _chunkIndex.Clear();

            if (!File.Exists(_chunkIndexPath))
                return;

            using FileStream stream = File.OpenRead(_chunkIndexPath);
            using BinaryReader reader = new(stream);

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                int x = reader.ReadInt32();
                int z = reader.ReadInt32();
                string hash = reader.ReadString();
                _chunkIndex[(x, z)] = hash; 
            }
        }
    }
}