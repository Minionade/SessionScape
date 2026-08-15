using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class ChunkGridRenderer
{
    static ChunkGridRenderer()
    {
        SceneView.duringSceneGui += SceneView_duringSceneGui;
    }

    private static void SceneView_duringSceneGui(SceneView sceneView)
    {
        Transform selectedTransform = Selection.activeTransform;

        if (selectedTransform == null)
            return;

        ChunkObject chunk = selectedTransform.GetComponentInParent<ChunkObject>();

        if (chunk == null)
            return;

        if (selectedTransform == chunk.transform)
            return;

        RenderGrid(chunk);
    }

    private static void RenderGrid(ChunkObject chunk)
    {
        int size = WorldConstants.ChunkSize;
        float tileSize = WorldConstants.TileSize;

        Vector3 origin = chunk.transform.position;
        float halfSize = size * tileSize * 0.5f;

        Handles.color = Color.black;

        CompareFunction previousZTest = Handles.zTest;
        Handles.zTest = CompareFunction.LessEqual;

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 start = GetVertexPosition(chunk, x, z, origin, halfSize, tileSize);

                Vector3 end = GetVertexPosition(chunk, x, z + 1, origin, halfSize, tileSize);

                Handles.DrawLine(start, end);
            }
        }

        for (int z = 0; z <= size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 start = GetVertexPosition(chunk, x, z, origin, halfSize, tileSize);

                Vector3 end = GetVertexPosition(chunk, x + 1, z, origin, halfSize, tileSize);

                Handles.DrawLine(start, end);
            }
        }

        Handles.zTest = previousZTest;
    }

    private static Vector3 GetVertexPosition(ChunkObject chunk, int x, int z, Vector3 origin, float halfSize, float tileSize)
    {
        float worldX = origin.x - halfSize + x * tileSize;
        float worldZ = origin.z - halfSize + z * tileSize;

        float height = GetVertexHeight(chunk, x, z);

        return new Vector3(worldX, origin.y + height, worldZ);
    }

    private static float GetVertexHeight(ChunkObject chunk, int x, int z)
    {
        return chunk.Data.GetVertexHeight(x, z);
    }
}