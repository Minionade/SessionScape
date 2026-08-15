using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SessionScape.Main.World
{
    public static class MapManifestSerializer
    {
        public static string ComputeChunkHash(byte[] chunkData)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(chunkData);
            return BytesToHex(hash);
        }

        public static string ComputeMapHash(List<ChunkManifest> chunks)
        {
            List<ChunkManifest> sorted = new(chunks);

            sorted.Sort((a, b) =>
            {
                int xCompare = a.ChunkX.CompareTo(b.ChunkX);
                if (xCompare != 0)
                    return xCompare;
                return a.ChunkZ.CompareTo(b.ChunkZ);
            });

            StringBuilder builder = new();
            foreach (ChunkManifest chunk in sorted)
            {
                builder.Append(chunk.ChunkX);
                builder.Append(",");
                builder.Append(chunk.ChunkZ);
                builder.Append(":");
                builder.Append(chunk.ContentHash);
                builder.Append(";");
            }

            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());

            using SHA256 sha256 = SHA256.Create();
            {
                byte[] hash = sha256.ComputeHash(data);

                return BytesToHex(hash);
            }
        }
        
        public static byte[] Serialize(MapManifest manifest)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(manifest.MapId);
            writer.Write(manifest.FormatVersion);
            writer.Write(manifest.MapVersion);
            writer.Write(manifest.ContentHash);
            writer.Write(manifest.Chunks.Count);

            foreach (ChunkManifest chunk in manifest.Chunks)
            {
                writer.Write(chunk.ChunkX);
                writer.Write(chunk.ChunkZ);
                writer.Write(chunk.ContentHash);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static MapManifest Deserialize(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            MapManifest manifest = new MapManifest
            {
                MapId = reader.ReadString(),
                FormatVersion = reader.ReadInt32(),
                MapVersion = reader.ReadInt32(),
                ContentHash = reader.ReadString()
            };

            int chunkCount = reader.ReadInt32();
            manifest.Chunks = new(chunkCount);

            for (int i = 0; i < chunkCount; i++)
            {
                ChunkManifest chunk = new()
                {
                    ChunkX = reader.ReadInt32(),
                    ChunkZ = reader.ReadInt32(),
                    ContentHash = reader.ReadString(),
                };

                manifest.Chunks.Add(chunk);
            }

            return manifest;
        }

        private static string BytesToHex(byte[] bytes)
        {
            StringBuilder builder = new(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}