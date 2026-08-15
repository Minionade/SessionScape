using System;
using System.IO;
using System.Security.Cryptography;

namespace SessionScape.Main.World
{
    public static class ChunkHasher
    {
        public static string ComputeHash(ChunkData chunk)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8);

            writer.Write(chunk.X);
            writer.Write(chunk.Z);
            writer.Write(chunk.SavedAtUtcTicks);

            foreach (var vertex in chunk.Vertexmap)
            {
                writer.Write(vertex.Height);
                writer.Write(vertex.R);
                writer.Write(vertex.G);
                writer.Write(vertex.B);
                writer.Write(vertex.A);
            }

            foreach (var tile in chunk.Tilemap)
            {
                writer.Write(tile.Walkable);
                writer.Write((int)tile.Connections);
            }

            foreach (var asset in chunk.Assetmap)
            {
                writer.Write(asset.InstanceId);
                writer.Write(asset.AssetId);
                writer.Write(asset.AssetLabel);
                writer.Write(asset.X);
                writer.Write(asset.Y);
                writer.Write(asset.Z);
                writer.Write(asset.RotationY);
                writer.Write(asset.ScaleX);
                writer.Write(asset.ScaleY);
                writer.Write(asset.ScaleZ);
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream.ToArray());
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}