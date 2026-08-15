using SessionScape.Main.World;
using SessionScape.Server.Simulation.World;

namespace Server.Simulation.World
{
    public class MapLoader
    {
        private const int SUPPORTED_FORMAT_VERSION = 1;

        private readonly string _mapDirectory;

        public MapManifest Manifest { get; private set; }

        public MapLoader(string mapDirectory)
        {
            _mapDirectory = mapDirectory ?? throw new ArgumentNullException(nameof(mapDirectory));
        }

        public void LoadMap(WorldMap worldMap)
        {
            ArgumentNullException.ThrowIfNull(worldMap);

            LoadManifest();
            ValidateManifest();

            HashSet<(int x, int z)> loadedChunks = new();

            foreach (ChunkManifest chunkManifest in Manifest.Chunks)
            {
                if (!loadedChunks.Add((chunkManifest.ChunkX, chunkManifest.ChunkZ)))
                {
                    throw new InvalidDataException($"Duplicate chunk found in manifest at ({chunkManifest.ChunkX}, {chunkManifest.ChunkZ})");
                }

                ChunkData chunkData = LoadChunk(chunkManifest.ChunkX, chunkManifest.ChunkZ);
                Chunk chunk = new(chunkData, worldMap, chunkManifest.ContentHash);

                worldMap.RegisterChunk(chunkData.X, chunkData.Z, chunk);
            }
        }

        private void LoadManifest()
        {
            string manifestPath = Path.Combine(_mapDirectory, "manifest.bin");

            Console.WriteLine($"Loading map file at {manifestPath}");

            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Map manifest file could not be found.", manifestPath);
            }

            byte[] manifestBytes = File.ReadAllBytes(manifestPath);
            Manifest = MapManifestSerializer.Deserialize(manifestBytes);

            string calculatedHash = MapManifestSerializer.ComputeMapHash(Manifest.Chunks);

            if (!string.Equals(calculatedHash, Manifest.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Map manifest content hash validation failed.");
            }
        }

        private void ValidateManifest()
        {
            if (Manifest == null)
            {
                throw new InvalidDataException("Map manifest is null.");
            }
            if (string.IsNullOrWhiteSpace(Manifest.MapId))
            {
                throw new InvalidDataException("Map manifest has no MapId.");
            }

            if (Manifest.Chunks == null)
            {
                throw new InvalidDataException("Map manifest has no chunk list.");
            }

            if (Manifest.FormatVersion !=
                SUPPORTED_FORMAT_VERSION)
            {
                throw new InvalidDataException(
                    $"Unsupported map format version " +
                    $"{Manifest.FormatVersion}. " +
                    $"Server supports version " +
                    $"{SUPPORTED_FORMAT_VERSION}.");
            }

            string calculatedMapHash = MapManifestSerializer.ComputeMapHash(Manifest.Chunks);
            if (!string.Equals(calculatedMapHash, Manifest.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Map manifest content has validation failed.");
            }
        }

        private ChunkData LoadChunk(int chunkX, int chunkZ)
        {
            if (Manifest == null)
            {
                throw new InvalidOperationException("LoadManifest() must be called before loading chunks.");
            }

            ChunkManifest chunkManifest = Manifest.Chunks.Find(chunk => chunk.ChunkX == chunkX && chunk.ChunkZ == chunkZ);

            if (chunkManifest == null)
            {
                throw new FileNotFoundException($"Chunk ({chunkX}, {chunkZ}): Does not exist in the map manifest.");
            }

            string chunkPath = MapFileUtility.GetChunkPath(_mapDirectory, chunkX, chunkZ);

            if (!File.Exists(chunkPath))
            {
                throw new FileNotFoundException($"Chunk file does not exist: {chunkPath}");
            }

            byte[] chunkBytes = File.ReadAllBytes(chunkPath);

            string calculatedHash = MapManifestSerializer.ComputeChunkHash(chunkBytes);

            if (!string.Equals(calculatedHash, chunkManifest.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Chunk ({chunkX}, {chunkZ}): Content hash validation failed.");
            }

            ChunkData chunkData = ChunkBinarySerializer.Deserialize(chunkBytes);

            if (chunkX != chunkData.X || chunkZ != chunkData.Z)
            {
                throw new InvalidDataException($"Chunk coordinate mismatch.  Expected: ({chunkX}, {chunkZ}) Found: ({chunkData.X}, {chunkData.Z})");
            }

            return chunkData;
        }
    }
}
