using System;
using System.IO;

namespace SessionScape.Main.World
{
    public static class ChunkBinarySerializer
    {
        public static byte[] Serialize(ChunkData chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            if (chunk.Vertexmap == null)
                throw new InvalidDataException(
                    "Chunk Vertexmap cannot be null.");

            if (chunk.Tilemap == null)
                throw new InvalidDataException(
                    "Chunk Tilemap cannot be null.");

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            // Chunk coordinates
            writer.Write(chunk.X);
            writer.Write(chunk.Z);

            // Vertex data
            writer.Write(chunk.Vertexmap.Length);

            foreach (VertexData vertex in chunk.Vertexmap)
            {
                writer.Write(vertex.Height);

                writer.Write(vertex.R);
                writer.Write(vertex.G);
                writer.Write(vertex.B);
                writer.Write(vertex.A);
            }

            // Tile data
            writer.Write(chunk.Tilemap.Length);

            foreach (TileData tile in chunk.Tilemap)
            {
                writer.Write(tile.Walkable);
                writer.Write((int)tile.Connections);
            }

            writer.Write(chunk.Assetmap.Length);

            foreach (AssetInstanceData asset in chunk.Assetmap)
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

            writer.Flush();

            return stream.ToArray();
        }

        public static ChunkData Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                throw new InvalidDataException("Chunk data is empty.");

            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            ChunkData chunk = new()
            {
                X = reader.ReadInt32(),
                Z = reader.ReadInt32()
            };

            // Vertex data
            int vertexCount = reader.ReadInt32();

            if (vertexCount < 0)
            {
                throw new InvalidDataException("Chunk contains an invalid vertex count.");
            }

            chunk.Vertexmap =
                new VertexData[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                chunk.Vertexmap[i] = new VertexData
                {
                    Height = reader.ReadSingle(),

                    R = reader.ReadByte(),
                    G = reader.ReadByte(),
                    B = reader.ReadByte(),
                    A = reader.ReadByte()
                };
            }

            // Tile data
            int tileCount = reader.ReadInt32();

            if (tileCount < 0)
            {
                throw new InvalidDataException("Chunk contains an invalid tile count.");
            }

            chunk.Tilemap = new TileData[tileCount];

            for (int i = 0; i < tileCount; i++)
            {
                chunk.Tilemap[i] = new TileData
                {
                    Walkable = reader.ReadBoolean(),
                    Connections =
                        (TileConnections)reader.ReadInt32()
                };
            }

            int assetCount = reader.ReadInt32();
            chunk.Assetmap = new AssetInstanceData[assetCount];

            for (int i = 0; i < assetCount; i++)
            {
                chunk.Assetmap[i] = new AssetInstanceData
                {

                    InstanceId = reader.ReadInt32(),
                    AssetId = reader.ReadInt32(),
                    AssetLabel = reader.ReadString(),
                    X = reader.ReadSingle(),
                    Y = reader.ReadSingle(),
                    Z = reader.ReadSingle(),
                    RotationY = reader.ReadSingle(),
                    ScaleX = reader.ReadSingle(),
                    ScaleY = reader.ReadSingle(),
                    ScaleZ = reader.ReadSingle(),
                };
            }

            // Make sure there is no unexpected data at the end.
            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("Chunk contains unexpected trailing data.");
            }

            return chunk;
        }
    }
}