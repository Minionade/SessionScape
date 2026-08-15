using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SessionScape.Client.Assets.Editor
{
    public static class MapFileExporter
    {
        public static MapManifest Export(MapBuilder map, string mapId,
            int formatVersion, int mapVersion, string outputDirectory)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));
            }

            if (map.Chunkmap.Count == 0)
            {
                throw new InvalidOperationException("Cannot export a map with 0 chunks.");
            }

            string mapDirectory = Path.Combine(outputDirectory, mapId);

            string chunksDirectory = Path.Combine(mapDirectory, "chunks");

            Directory.CreateDirectory(mapDirectory);

            Directory.CreateDirectory(chunksDirectory);

            List<ChunkManifest> chunkManifests = new();

            IEnumerable<KeyValuePair<(int x, int z), ChunkObject>> chunks = map.Chunkmap
                    .OrderBy(pair => pair.Key.x)
                    .ThenBy(pair => pair.Key.z);

            foreach (KeyValuePair<(int x, int z), ChunkObject> pair in chunks)
            {
                ChunkObject chunkObject = pair.Value;

                if (chunkObject == null)
                {
                    throw new InvalidOperationException($"Chunk at ({pair.Key.x}, {pair.Key.z}) is null.");
                }

                ChunkData chunkData = chunkObject.Data ?? throw new InvalidOperationException($"Chunk at ({pair.Key.x}, {pair.Key.z}) has no ChunkData.");

                if (chunkData.X != pair.Key.x || chunkData.Z != pair.Key.z)
                {
                    throw new InvalidDataException(
                        $"Chunk coordinate mismatch. " +
                        $"MapBuilder has ({pair.Key.x}, {pair.Key.z}) " +
                        $"but ChunkData contains " +
                        $"({chunkData.X}, {chunkData.Z}).");
                }

                if (!chunkObject.BakeAssets(out List<string> assetErrors))
                {
                    throw new InvalidDataException(
                        $"Chunk at ({pair.Key.x}, {pair.Key.z}) has invalid assets:\n" +
                        string.Join("\n", assetErrors));
                }

                byte[] chunkBytes = ChunkBinarySerializer.Serialize(chunkData);

                string chunkHash = MapManifestSerializer.ComputeChunkHash(chunkBytes);

                string chunkPath = MapFileUtility.GetChunkPath(mapDirectory, chunkData.X, chunkData.Z);

                File.WriteAllBytes(chunkPath, chunkBytes);

                ChunkManifest chunkManifest = new()
                    {
                        ChunkX = chunkData.X,
                        ChunkZ = chunkData.Z,
                        ContentHash = chunkHash
                    };

                chunkManifests.Add(chunkManifest);
            }

            HashSet<string> validFileNames = chunks
                .Select(pair => MapFileUtility.GetChunkFileName(pair.Key.x, pair.Key.z))
                .ToHashSet();

            foreach (string existingFile in Directory.GetFiles(chunksDirectory, "*.bin"))
            {
                if (!validFileNames.Contains(Path.GetFileName(existingFile)))
                {
                    File.Delete(existingFile);
                    File.Delete(existingFile + ".meta");
                }
            }

            MapManifest manifest = new()
                {
                    MapId = mapId,
                    FormatVersion = formatVersion,
                    MapVersion = mapVersion,
                    Chunks = chunkManifests
                };

            manifest.ContentHash = MapManifestSerializer.ComputeMapHash(manifest.Chunks);

            byte[] manifestBytes = MapManifestSerializer.Serialize(manifest);

            string manifestPath = Path.Combine(mapDirectory, "manifest.bin");

            File.WriteAllBytes(manifestPath, manifestBytes);

            return manifest;
        }
    }
}