using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEngine;

namespace SessionScape.Client.Assets.Scripts.World
{
    public class MapLoader : MonoBehaviour, IChunkOwner
    {
        [SerializeField] private AssetRegistry assetRegistry;

        public Dictionary<(int x, int z), ChunkObject> Chunkmap { get; } = new();

        public bool TryGetChunk(int x, int z, out ChunkObject chunk) => Chunkmap.TryGetValue((x, z), out chunk);

        public bool TryGetChunk(Vector3 worldPosition, out ChunkObject chunk)
        {
            float chunkWorldSize = WorldConstants.ChunkSize * WorldConstants.TileSize;
            float chunkWorldHalf = chunkWorldSize / 2;

            int chunkX = Mathf.FloorToInt((worldPosition.x +  chunkWorldHalf) / chunkWorldSize);
            int chunkZ = Mathf.FloorToInt((worldPosition.z + chunkWorldHalf) / chunkWorldSize);

            return TryGetChunk(chunkX, chunkZ, out chunk);
        }

        public bool TryGetTile(Vector3 worldPosition, out (int x, int z) tileCoords, out TileData tile)
        {
            tileCoords = default;
            tile = default;

            if (!TryGetChunk(worldPosition, out ChunkObject chunk))
                return false;

            if (!GetTileCoordinate(worldPosition, out tileCoords.x, out tileCoords.z))
                return false;

            int index = chunk.Data.GetTileIndex(tileCoords.x, tileCoords.z);

            if (index < 0 || index >= chunk.Data.Tilemap.Length)
                return false;

            tile = chunk.Data.Tilemap[index];

            return true;
        }

        private bool GetTileCoordinate(Vector3 worldPosition, out int x, out int z)
        {
            x = -1;
            z = -1;

            float chunkWorldSize = WorldConstants.ChunkSize * WorldConstants.TileSize;
            float chunkWorldHalf = chunkWorldSize / 2f;

            if (!TryGetChunk(worldPosition, out ChunkObject chunk))
                return false;

            float localX = worldPosition.x - (chunk.Data.X * chunkWorldSize);
            float localZ = worldPosition.z - (chunk.Data.Z * chunkWorldSize);

            localX += chunkWorldHalf;
            localZ += chunkWorldHalf;

            x = Mathf.FloorToInt(localX / WorldConstants.TileSize);
            z = Mathf.FloorToInt(localZ / WorldConstants.TileSize);

            return x >= 0 &&
                   x < WorldConstants.ChunkSize &&
                   z >= 0 &&
                   z < WorldConstants.ChunkSize;
        }

        public void LoadChunk(ChunkData data)
        {
            (int x, int z) = (data.X, data.Z);

            if (Chunkmap.TryGetValue((x, z), out ChunkObject existing))
            {
                existing.LoadFromData(this, data);
                return;
            }

            GameObject chunkGameObject = new($"Chunk ({x}, {z})");
            chunkGameObject.transform.SetParent(transform);

            float chunkSize = WorldConstants.ChunkSize * WorldConstants.TileSize;
            chunkGameObject.transform.position = new Vector3(x * chunkSize, 0, z * chunkSize);

            ChunkObject chunkObject = chunkGameObject.AddComponent<ChunkObject>();
            chunkObject.LoadFromData(this, data);

            foreach (AssetInstanceData asset in data.Assetmap)
            {
                if (!assetRegistry.TryGetPrefab(asset.AssetId, out GameObject prefab))
                    continue;

                GameObject instance = Instantiate(prefab, chunkGameObject.transform);
                instance.transform.localPosition = new Vector3(asset.X, asset.Y, asset.Z);
                instance.transform.localRotation = Quaternion.Euler(0, asset.RotationY, 0);
                instance.transform.localScale = new Vector3(asset.ScaleX, asset.ScaleY, asset.ScaleZ);
            }

            Chunkmap[(x, z)] = chunkObject;
        }

        public void UnloadChunk(int x, int z)
        {
            if (Chunkmap.TryGetValue((x, z), out ChunkObject chunk))
            {
                Object.Destroy(chunk.gameObject);
                Chunkmap.Remove((x, z));
            }
        }

        public void LoadChunks(IEnumerable<ChunkData> chunks)
        {
            foreach (ChunkData data in chunks)
            {
                LoadChunk(data);
            }
        }
    }
}
