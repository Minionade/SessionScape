using SessionScape.Main.World;
using UnityEngine;

namespace SessionScape.Client.Assets.Scripts.World
{
    public static class ChunkMathHelper
    {
        public static Vector3 LocalVertexToWorldPosition(ChunkData chunk, int localX, int localZ)
        {
            float tileSize = WorldConstants.TileSize;
            float chunkWidth = WorldConstants.ChunkSize * tileSize;
            float offset = chunkWidth * 0.5f;

            Vector3 localPosition = new Vector3
            {
                x = (localX * tileSize) - offset,
                y = chunk.GetVertexHeight(localX, localZ),
                z = (localZ * tileSize) - offset
            };

            Vector3 chunkPosition = new Vector3
            {
                x = chunk.X * chunkWidth,
                y = 0f,
                z = chunk.Z * chunkWidth
            };

            return chunkPosition + localPosition;
        }

        public static Vector3 WorldVertexToVector3(ChunkData chunk, int worldX, int worldZ)
        {
            TileCoordinateMath.WorldToLocalVertex(worldX, worldZ, out int localX, out int localZ);

            return new Vector3
            {
                x = worldX,
                y = chunk.GetVertexHeight(localX, localZ),
                z = worldZ
            };
        }

        public static bool Vector3ToLocalVertex(Vector3 worldPosition, ChunkData chunk, out int localX, out int localZ)
        {
            int chunkSize = WorldConstants.ChunkSize;
            float tileSize = WorldConstants.TileSize;
            float chunkWidth = chunkSize * tileSize;

            Vector3 chunkPositon = new Vector3
            {
                x = chunk.X * chunkWidth,
                y = 0f,
                z = chunk.Z * chunkWidth
            };

            Vector3 localPosition = worldPosition - chunkPositon;
            float offset = chunkWidth * 0.5f;

            localPosition.x += offset;
            localPosition.z += offset;

            localX = Mathf.RoundToInt(localPosition.x / tileSize);
            localZ = Mathf.RoundToInt(localPosition.z / tileSize);

            return localX >= 0 && localX <= chunkSize &&
                   localZ >= 0 && localZ <= chunkSize;
        }
    }
}