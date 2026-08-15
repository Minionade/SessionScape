using SessionScape.Main.World;
using SessionScape.Client.Assets.Scripts.World;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SessionScape.Client.Assets.Editor
{
    public static class ChunkEditorHelper
    {
        public static void DestroyMesh(ref Mesh mesh)
        {
            if (mesh == null)
                return;

            Object.DestroyImmediate(mesh);
            mesh = null;
        }

        public static void DestroyMaterial(ref Material material)
        {
            if (material == null)
                return;

            Object.DestroyImmediate(material);
            material = null;
        }

        public static Vector3 GetMouseVertexPosition(ChunkObject chunk, Vector2 screenPosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(screenPosition);
            float closestDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;

            for (int x = 0; x <= WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z <= WorldConstants.ChunkSize; z++)
                {
                    Vector3 vertexPosition = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);
                    float intersect = Vector3.Dot(vertexPosition - ray.origin, ray.direction);
                    intersect = Mathf.Max(0, intersect);

                    Vector3 rayPoint = ray.origin + ray.direction * intersect;
                    float distance = Vector3.Distance(vertexPosition, rayPoint);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPoint = vertexPosition;
                    }
                }
            }

            return (closestDistance < 2f) ? closestPoint : Vector3.zero;
        }

        public static bool IsVertexVisible(Vector3 worldPosition, Camera camera)
        {
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);

            return screenPosition.z > 0 &&
                screenPosition.y > -50 && screenPosition.y < camera.pixelWidth + 50 &&
                screenPosition.z > -50 && screenPosition.y < camera.pixelHeight + 50;
        }
    }
}