using SessionScape.Main.World;
using UnityEngine;

namespace SessionScape.Client.Assets.Scripts.World
{
    public class TileHighlight : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private Transform targetTransform;

        private MapLoader _mapLoader;

        private void Update()
        {
            if (!targetTransform)
                return;
        }

        public void Initialize(MapLoader mapLoader)
        {
            _mapLoader = mapLoader;
            if (!meshRenderer)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (!meshFilter)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer.material = ChunkMeshGenerator.GeneratePriorityVertexColorMaterial();
        }

        public void SetTile(Vector3 position)
        {
            if (!_mapLoader.TryGetChunk(position, out ChunkObject chunk))
                return;

            if (!_mapLoader.TryGetTile(position, out (int x, int z) tileCoords, out TileData tile))
                return;

            Vector3 v00 = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, tileCoords.x, tileCoords.z);
            Vector3 v10 = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, tileCoords.x + 1, tileCoords.z);
            Vector3 v01 = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, tileCoords.x, tileCoords.z + 1);
            Vector3 v11 = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, tileCoords.x + 1, tileCoords.z + 1);

            v00.y += 0.01f;
            v10.y += 0.01f;
            v01.y += 0.01f;
            v11.y += 0.01f;

            Mesh mesh = meshFilter.mesh;

            mesh.vertices = new[]
            {
                transform.InverseTransformPoint(v00),
                transform.InverseTransformPoint(v01),
                transform.InverseTransformPoint(v10),
                transform.InverseTransformPoint(v11)
            };

            mesh.triangles = new[]
            {
                0, 1, 2,
                2, 1, 3
            };

            mesh.RecalculateBounds();
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void SetColor(Color color)
        {
            meshRenderer.material.color = color;
        }

        public void Enable()
        {
            meshRenderer.enabled = true;
        }

        public void Disable()
        {
            meshRenderer.enabled = false;
        }

        public void SetDestroyTarget(Transform transform)
        {
            targetTransform = transform;
        }
    }
}