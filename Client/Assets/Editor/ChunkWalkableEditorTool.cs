using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace SessionScape.Client.Assets.Editor
{
    [EditorTool("Chunk Walkability Painter", typeof(ChunkObject))]
    public class ChunkWalkableEditorTool : EditorTool
    {
        private ChunkObject chunk;

        private Vector2Int marqueeStart;
        private Vector2Int marqueeCurrent;
        private bool isMarqueeSelecting = false;

        private bool isPainting = false;
        private bool brushSetting = false;

        private Mesh previewGridMesh;
        private Material lineMaterial;

        private Mesh previewSolidMesh;
        private Material solidMaterial;

        private Mesh brushMesh;
        private Material brushMaterial;
        private int lastX, lastZ;

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("Exposure");

        public override void OnToolGUI(EditorWindow window)
        {
            chunk = target as ChunkObject;

            if (chunk == null)
                return;

            if (chunk.Data == null)
                return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            RenderGridMesh();
            RenderSolidMesh();
            RenderBrush();
            HandleInput();
            RenderMarqueeSelection(marqueeStart, marqueeCurrent);
        }

        public override void OnActivated()
        {
            isPainting = false;
            isMarqueeSelecting = false;
            
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;

            ChunkEditorHelper.DestroyMesh(ref previewGridMesh);
            ChunkEditorHelper.DestroyMesh(ref previewSolidMesh);
            ChunkEditorHelper.DestroyMesh(ref brushMesh);

            ChunkEditorHelper.DestroyMaterial(ref lineMaterial);
            ChunkEditorHelper.DestroyMaterial(ref solidMaterial);
            ChunkEditorHelper.DestroyMaterial(ref brushMaterial);
        }

        private void RenderGridMesh()
        {
            if (chunk.Data == null)
                return;

            if (previewGridMesh == null)
                previewGridMesh = ChunkMeshGenerator.GenerateGridMesh("Preview Grid Mesh", chunk.Data);

            if (lineMaterial == null)
                lineMaterial = ChunkMeshGenerator.GenerateLineMaterial();

            Graphics.DrawMesh(previewGridMesh, chunk.transform.localToWorldMatrix, lineMaterial, 0);
        }

        private void RenderSolidMesh()
        {
            if (previewSolidMesh == null)
                previewSolidMesh = GenerateSolidPreviewMesh();
            if (solidMaterial == null)
                solidMaterial = ChunkMeshGenerator.GeneratePriorityVertexColorMaterial();

            if (SceneView.currentDrawingSceneView == null)
                return;

            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;

            Graphics.DrawMesh(previewSolidMesh, chunk.transform.localToWorldMatrix, solidMaterial, 0, sceneCamera);
        }

        private void RenderBrush()
        {
            Event current = Event.current;

            if (!TryGetTileUnderMouse(current.mousePosition, out int x, out int z))
            {
                ChunkMeshGenerator.DestroyMesh(ref brushMesh);
                return;
            }

            RenderBrushMesh(x, z);
            SceneView.RepaintAll();
        }

        private void RenderBrushMesh(int x, int z)
        {
            if (brushMaterial == null)
            {
                brushMaterial = ChunkMeshGenerator.GeneratePriorityVertexColorMaterial();
            }
            if (lastX != x || lastZ != z)
            {
                brushMesh = ChunkMeshGenerator.GenerateSingleTileMesh(chunk.Data, x, z, Color.yellow);
            }

            Graphics.DrawMesh(brushMesh, chunk.transform.localToWorldMatrix, brushMaterial, 0);
        }

        private void RenderMarqueeSelection(Vector2Int marqueeStart, Vector2Int marqueeEnd)
        {
            if (!isMarqueeSelecting)
                return;

            int minX = Mathf.Min(marqueeStart.x, marqueeCurrent.x);
            int maxX = Mathf.Max(marqueeStart.x, marqueeCurrent.x);
            int minZ = Mathf.Min(marqueeStart.y, marqueeCurrent.y);
            int maxZ = Mathf.Max(marqueeStart.y, marqueeCurrent.y);

            Vector3 minWorld = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, minX, minZ);
            Vector3 maxWorld = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, maxX + 1, maxZ + 1);
            Vector3 center = (minWorld + maxWorld) * 0.5f;

            float sizeX = (maxX - minX + 1) * WorldConstants.TileSize;
            float sizeZ = (maxZ - minZ + 1) * WorldConstants.TileSize;
            Vector3 size = new Vector3(sizeX, 0.1f, sizeZ);

            Handles.color = Color.blue;
            Handles.DrawWireCube(center, size);
        }

        private void HandleInput()
        {
            Event current = Event.current;

            HandleMouseDown(current);
            HandleMouseDrag(current);
            HandleMouseUp(current);
        }

        private void HandleMouseDown(Event current)
        {
            if (current.type != EventType.MouseDown)
                return;

            if (isMarqueeSelecting && current.button == 1)
            {
                isMarqueeSelecting = false;
                current.Use();
            }
            else if (current.button == 0 && TryGetTileUnderMouse(current.mousePosition, out int x, out int z))
            {
                if (current.shift)
                {
                    isMarqueeSelecting = true;
                    brushSetting = !chunk.Data.IsTileWalkable(x, z);
                    marqueeStart = new Vector2Int(x, z);
                    marqueeCurrent = marqueeStart;

                    current.Use();
                }
                else
                {
                    isPainting = true;
                    brushSetting = !chunk.Data.IsTileWalkable(x, z);
                    PaintAtMouse(current.mousePosition);

                    current.Use();
                }
            }
        }

        private void HandleMouseDrag(Event current)
        {
            if (current.type != EventType.MouseDrag)
                return;

            if (isMarqueeSelecting && TryGetTileUnderMouse(current.mousePosition, out int x, out int z))
            {
                marqueeCurrent = new Vector2Int(x, z);

                current.Use();
            }
            else if (isPainting && current.button == 0)
            {
                PaintAtMouse(current.mousePosition);

                current.Use();
            }
        }

        private void HandleMouseUp(Event current)
        {
            if (current.type != EventType.MouseUp ||
                current.button != 0)
                return;

            if (isMarqueeSelecting)
            {
                isMarqueeSelecting = false;
                PaintMarquee(marqueeStart, marqueeCurrent);

                current.Use();
            }
            else if (isPainting)
            {
                isPainting = false;

                current.Use();
            }
        }

        private void PaintAtMouse(Vector2 mousePosition)
        {
            if (!TryGetTileUnderMouse(mousePosition, out int x, out int z))
                return;

            Undo.RecordObject(chunk, "Paint Walkable State");

            int index = chunk.Data.GetTileIndex(x, z);
            SetTileWalkable(index, brushSetting);

            RebuildMesh();
        }

        private void PaintMarquee(Vector2Int startPosition, Vector2Int endPosition)
        {
            int minX = Mathf.Min(startPosition.x, endPosition.x);
            int maxX = Mathf.Max(startPosition.x, endPosition.x);
            int minZ = Mathf.Min(startPosition.y, endPosition.y);
            int maxZ = Mathf.Max(startPosition.y, endPosition.y);

            if (minX < 0 || maxX >= WorldConstants.ChunkSize ||
                minZ < 0 || maxZ >= WorldConstants.ChunkSize)
                return;

            Undo.RecordObject(chunk, "Paint Walkable States");
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z =  minZ; z <= maxZ; z++)
                {
                    int index = chunk.Data.GetTileIndex(x, z);
                    SetTileWalkable(index, brushSetting);
                }
            }

            RebuildMesh();
        }

        private void SetTileWalkable(int index, bool brushSetting)
        {
            TileData tile = chunk.Data.Tilemap[index];
            tile.Walkable = brushSetting;
            chunk.Data.Tilemap[index] = tile;
        }

        private Mesh GenerateSolidPreviewMesh()
        {
            Mesh mesh = ChunkMeshGenerator.GenerateChunkMesh(chunk.Data);

            List<Color> colors = new();
            for (int x = 0; x < WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z < WorldConstants.ChunkSize; z++)
                {
                    bool walkable = chunk.Data.IsTileWalkable(x, z);
                    Color tileColor = walkable ? new Color(1, 1, 1, 0.2f) : new Color(1, 0, 0, 0.2f);

                    colors.Add(tileColor);
                    colors.Add(tileColor);
                    colors.Add(tileColor);
                    colors.Add(tileColor);
                    colors.Add(tileColor);
                    colors.Add(tileColor);
                }
            }

            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void RebuildMesh()
        {
            ChunkMeshGenerator.DestroyMesh(ref previewGridMesh);
            ChunkMeshGenerator.DestroyMesh(ref previewSolidMesh);
            ChunkMeshGenerator.DestroyMesh(ref brushMesh);

            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
        }

        private bool TryGetTileUnderMouse(Vector2 mousePosition, out int outX, out int outZ)
        {
            outX = outZ = -1;
            if (!chunk.TryGetComponent(out MeshCollider collider))
                return false;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (!collider.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
                return false;

            int x = Mathf.FloorToInt(hitInfo.point.x);
            int z = Mathf.FloorToInt(hitInfo.point.z);
            TileCoordinateMath.WorldToLocalTile(x, z, out outX, out outZ);
            return true;
        }

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (brushMesh == null || brushMaterial == null || chunk == null)
                return;

            //Graphics.DrawMesh(brushMesh, chunk.transform.localToWorldMatrix, brushMaterial, 0);
        }
    }
}