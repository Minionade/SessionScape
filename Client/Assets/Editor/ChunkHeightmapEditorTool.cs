using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace SessionScape.Client.Assets.Editor
{
    [EditorTool("Chunk Heightmap Editor", typeof(ChunkObject))]
    public class ChunkHeightmapEditorTool : EditorTool
    {
        private const float HANDLE_SIZE = 0.04f;

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("d_RectTool On");

        private ChunkObject chunk;

        private HashSet<(int x, int z)> selectedVertices = new();

        private Mesh previewGridMesh;
        private Material lineMaterial;

        private Rect guiRect = new Rect(10, 10, 140, 140);

        private Vector2Int marqueeStart;
        private Vector2Int marqueeCurrent;
        private bool isMarqueeSelecting;

        private (int x, int z)? draggedVertex = null;

        public override void OnToolGUI(EditorWindow window)
        {
            chunk = target as ChunkObject;
            
            if (chunk == null)
                return;

            if (chunk.Data == null)
                return;

            RenderSelectionHandles();
            RenderGridMesh();
            HandleInput();
            RenderMarqueeSelection();
            RenderGUI();
        }

        public override void OnActivated()
        {
            isMarqueeSelecting = false;

            EditorApplication.update += EditorApplication_update;
            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        public override void OnWillBeDeactivated()
        {
            EditorApplication.update -= EditorApplication_update;
            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;

            ChunkEditorHelper.DestroyMesh(ref previewGridMesh);
            ChunkEditorHelper.DestroyMaterial(ref lineMaterial);
        }

        private void RenderSelectionHandles()
        {
            if (SceneView.currentDrawingSceneView == null)
                return;

            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;

            Handles.color = Color.white;
            int chunkSize = WorldConstants.ChunkSize;

            for (int x = 0; x <= chunkSize; x++)
            {
                for (int z = 0; z <= chunkSize; z++)
                { 
                    Vector3 worldPosition = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);

                    if (!ChunkEditorHelper.IsVertexVisible(worldPosition, sceneCamera))
                        continue;

                    bool isVertexSelected = selectedVertices.Contains((x, z));
                    Handles.color = isVertexSelected ? Color.yellow : Color.white;

                    float handleSize = HandleUtility.GetHandleSize(worldPosition) * HANDLE_SIZE;
                    int controlID = GUIUtility.GetControlID(FocusType.Passive);
                    Handles.SphereHandleCap(controlID, worldPosition, Quaternion.identity,
                        handleSize, EventType.Repaint);
                }
            }

            if (selectedVertices.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var (x, z) in selectedVertices)
                    center += ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);

                center /= selectedVertices.Count;

                Handles.color = Color.green;
                float handleSize = HandleUtility.GetHandleSize(center) * 0.5f;
                float snap = EditorSnapSettings.gridSnapEnabled ? EditorSnapSettings.move.y : 0.1f;

                Vector3 newCenter = Handles.Slider(center, Vector3.up, handleSize, Handles.ArrowHandleCap, snap);

                if (newCenter.y != center.y)
                {
                    float delta = newCenter.y - center.y;
                    AdjustSelectedHeights(delta);
                }
            }
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

        private void RenderGUI()
        {
            Handles.BeginGUI();

            guiRect = new Rect(10, 10, 240, 140);
            GUILayout.BeginArea(guiRect, GUI.skin.box);
            GUILayout.Label("Heightmap Editor", EditorStyles.boldLabel);

            if (selectedVertices.Count == 0)
            {
                GUILayout.Label("Selected Height: -");
                if (GUILayout.Button("Deselect All"))
                {
                    selectedVertices.Clear();
                }
                if (GUILayout.Button("Select All"))
                {
                    SelectAllVertices(Event.current);
                }
                if (GUILayout.Button("Reset"))
                {
                    for (int i = 0; i < chunk.Data.Vertexmap.Length; i++)
                    {
                        VertexData vertex = chunk.Data.Vertexmap[i];
                        vertex.Height = 0;
                        chunk.Data.Vertexmap[i] = vertex;
                    } 

                    RebuildMesh();
                }

                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            float sum = 0f;
            foreach (var (x, z) in selectedVertices)
            {
                sum += chunk.Data.GetVertexHeight(x, z);
            }

            float avg = sum / selectedVertices.Count;

            EditorGUI.BeginChangeCheck();

            string input = EditorGUILayout.TextField("Selected Height", avg.ToString());

            if (EditorGUI.EndChangeCheck())
            {
                input = input.Trim();

                if (float.TryParse(input, out float parsedValue))
                {
                    SetSelectedHeight(parsedValue);
                }
            }

            if (GUILayout.Button("Deselect All"))
            {
                selectedVertices.Clear();
            }
            if (GUILayout.Button("Select All"))
            {
                SelectAllVertices(Event.current);
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void RenderMarqueeSelection()
        {
            if (!isMarqueeSelecting)
                return;

            Event current = Event.current;
            Vector2 marqueeCurrentPosition = current.mousePosition;

            int minX = Mathf.Min(marqueeStart.x, marqueeCurrent.x);
            int maxX = Mathf.Max(marqueeStart.x, marqueeCurrent.x);
            int minZ = Mathf.Min(marqueeStart.y, marqueeCurrent.y);
            int maxZ = Mathf.Max(marqueeStart.y, marqueeCurrent.y);

            Vector3 minWorld = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, minX, minZ);
            Vector3 maxWorld = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, maxX, maxZ);
            Vector3 center = (minWorld + maxWorld) * 0.5f;

            float sizeX = (maxX - minX) * WorldConstants.TileSize;
            float sizeZ = (maxZ - minZ) * WorldConstants.TileSize;
            Vector3 size = new Vector3(sizeX, 0.1f, sizeZ);
            Handles.color = Color.blue;
            Handles.DrawWireCube(center, size);
        }

        private void HandleInput()
        {
            Event current = Event.current;

            if (guiRect.Contains(current.mousePosition))
                return;

            if (GUIUtility.hotControl != 0)
                return;

            if (current.type == EventType.KeyDown && current.control &&
                current.keyCode == KeyCode.A)
                SelectAllVertices(current);

            else if (current.type == EventType.MouseDown)
                HandleMouseDown(current);
            else if (current.type == EventType.MouseDrag)
                HandleMouseDrag(current);
            else if (current.type == EventType.MouseUp)
                HandleMouseUp(current);
        }

        private void HandleMouseDown(Event current)
        {
            if (current.button != 0 || isMarqueeSelecting)
                return;

            Vector3 mouseWorldPosition = ChunkEditorHelper.GetMouseVertexPosition(chunk, current.mousePosition);
            if (ChunkMathHelper.Vector3ToLocalVertex(mouseWorldPosition, chunk.Data, out int localX, out int localZ))
            {
                if (current.shift && GUIUtility.hotControl == 0)
                {
                    isMarqueeSelecting = true;
                    marqueeStart = new Vector2Int(localX, localZ);
                    marqueeCurrent = new Vector2Int(localX, localZ);

                    current.Use();
                }
                else if (current.control)
                {
                    var vertex = (localX, localZ);
                    if (selectedVertices.Contains(vertex))
                        selectedVertices.Remove(vertex);
                    else
                        selectedVertices.Add(vertex);

                    current.Use();
                }
                else
                {
                    selectedVertices.Clear();
                    selectedVertices.Add((localX, localZ));
                    draggedVertex = (localX, localZ);

                    current.Use();
                }
            }
        }

        private void HandleMouseDrag(Event current)
        {
            if (isMarqueeSelecting)
            {
                Vector3 mouseWorldPosition = ChunkEditorHelper.GetMouseVertexPosition(chunk, current.mousePosition);
                if (ChunkMathHelper.Vector3ToLocalVertex(mouseWorldPosition, chunk.Data, out int localX, out int localZ))
                {
                    marqueeCurrent = new Vector2Int(localX, localZ);

                    SceneView.RepaintAll();

                    current.Use();
                }
            }
            else if (draggedVertex.HasValue)
                current.Use();
        }

        private void HandleMouseUp(Event current)
        {
            if (isMarqueeSelecting)
            {
                SelectFromRect(marqueeStart, marqueeCurrent, current.shift);
                isMarqueeSelecting = false;

                current.Use();
            }

            draggedVertex = null;
            SceneView.RepaintAll();
        }

        private void SelectAllVertices(Event current)
        {
            selectedVertices.Clear();

            for (int x = 0; x <= WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z <= WorldConstants.ChunkSize; z++)
                {
                    selectedVertices.Add((x, z));
                }
            }

            current.Use();
        }

        private void SelectFromRect(Vector2Int rectStart, Vector2Int rectEnd, bool clearPrevious)
        {
            if (clearPrevious)
                selectedVertices.Clear();

            int minX = Mathf.Min(rectStart.x, rectEnd.x);
            int minZ = Mathf.Min(rectStart.y, rectEnd.y);
            int maxX = Mathf.Max(rectStart.x, rectEnd.x);
            int maxZ = Mathf.Max(rectStart.y, rectEnd.y);

            minX = Mathf.Max(0, minX);
            minZ = Mathf.Max(0, minZ);
            maxX = Mathf.Min(maxX, WorldConstants.ChunkSize);
            maxZ = Mathf.Min(maxZ, WorldConstants.ChunkSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    selectedVertices.Add((x, z));
                }
            }
        }

        private void SetSelectedHeight(float newHeight)
        {
            Undo.RecordObject(chunk, "Adjust Vertex Height");

            foreach (var (x, z) in selectedVertices)
            {
                float currentHeight = chunk.Data.GetVertexHeight(x, z);
                int index = chunk.Data.GetVertexIndex(x, z);
                VertexData data = chunk.Data.Vertexmap[index];
                data.Height = newHeight;
                chunk.Data.Vertexmap[index] = data;
            }

            RebuildMesh();
        }
        private void AdjustSelectedHeights(float delta)
        {
            Undo.RecordObject(chunk, "Adjust Vertex Height");

            foreach (var (x, z) in selectedVertices)
            {
                int index = chunk.Data.GetVertexIndex(x, z);
                VertexData vertex = chunk.Data.Vertexmap[index];
                vertex.Height += delta;
                chunk.Data.Vertexmap[index] = vertex;
            }

            RebuildMesh();
        }

        private void RebuildMesh()
        {
            ChunkEditorHelper.DestroyMesh(ref previewGridMesh);

            chunk.RebuildMesh();

            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
        }

        private void EditorApplication_update()
        {
            SceneView.RepaintAll();
        }

        private void Undo_undoRedoPerformed()
        {
            if (chunk == null)
                return;
            if (chunk.Data == null)
                return;

            chunk.RebuildMesh();

            ChunkEditorHelper.DestroyMesh(ref previewGridMesh);

            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
        }
    }
}