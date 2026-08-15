using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace SessionScape.Client.Assets.Editor
{
    [EditorTool("Chunk Vertex Painter", typeof(ChunkObject))]
    public class ChunkPainterEditorTool : EditorTool
    {
        private const float HANDLE_SIZE = 0.02f;
        private const float BRUSH_MAX_SIZE = 32f;
        private const float BRUSH_MIN_SIZE = 0.25f;

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("d_Grid.PaintTool");

        private ChunkObject chunk;

        private bool isPainting;
        private float brushSize = 0.5f;
        private Color brushColor = Color.lawnGreen;

        private Rect guiRect = new Rect(10, 10, 250, 140);

        public override void OnToolGUI(EditorWindow window)
        {
            chunk = target as ChunkObject;

            if (chunk == null)
                return;

            if (chunk.Data == null)
                return;

            RenderPreviewHandles();
            RenderBrush();
            HandleInput();
            RenderGUI();
        }

        public override void OnActivated()
        {
            isPainting = false;
        }

        private void RenderPreviewHandles()
        {
            if (SceneView.currentDrawingSceneView == null)
                return;

            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;

            // Vertex tool, use <= not <.
            for (int x = 0; x <= WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z <= WorldConstants.ChunkSize; z++)
                {
                    Vector3 worldPosition = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);

                    if (!ChunkEditorHelper.IsVertexVisible(worldPosition, sceneCamera))
                        continue;

                    chunk.Data.GetVertexColor(x, z, out var color);
                    Color vertexColor = new Color32(color.R, color.G, color.B, color.A);

                    Handles.color = vertexColor;
                    float handleSize = HandleUtility.GetHandleSize(worldPosition) * HANDLE_SIZE;
                    int controlID = GUIUtility.GetControlID(FocusType.Passive);

                    Handles.SphereHandleCap(controlID, worldPosition, Quaternion.identity, handleSize, EventType.Repaint);
                }
            }
        }

        private void RenderBrush()
        {
            Event current = Event.current;

            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = new Color(0, 0.8f, 1f, 0.5f); // cyan kinda transparent
                Handles.DrawWireDisc(hit.point, hit.normal, brushSize); // outline

                //Handles.color = new Color(0, 0.8f, 1f, 0.1f); // cyan more transparent
                //Handles.DrawSolidDisc(hit.point, hit.normal, brushSize * 0.1f); // center

                SceneView.RepaintAll();
            }   
        }

        private void RenderGUI()
        {
            Handles.BeginGUI();
            guiRect = new Rect(10, 10, 250, 140);
            GUILayout.BeginArea(guiRect, GUI.skin.box);

            GUILayout.Label("Vertex Paint Settings", EditorStyles.boldLabel);
            brushColor = EditorGUILayout.ColorField("Color", brushColor);
            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, BRUSH_MIN_SIZE, BRUSH_MAX_SIZE);

            if (GUILayout.Button("Fill Chunk"))
            {
                for (int i = 0; i < chunk.Data.Vertexmap.Length; i++)
                {
                    SetSingleColor(i, brushColor);
                }

                RebuildMesh();
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void HandleInput()
        {
            Event current = Event.current;

            if (guiRect.Contains(current.mousePosition))
                return;

            if (GUIUtility.hotControl != 0)
                return;

            if (current.type == EventType.ScrollWheel && current.control)
            {
                AdjustBrushSize(current);
                current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0)
            {
                isPainting = true;
                PaintAtMouse(current.mousePosition);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && isPainting)
            {
                PaintAtMouse(current.mousePosition);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                isPainting = false;
                current.Use();
            }
        }

        private void SetSingleColor(int index, Color32 color)
        {
            VertexData vertex = chunk.Data.Vertexmap[index];
            vertex.R = color.r;
            vertex.G = color.g;
            vertex.B = color.b;
            vertex.A = color.a;
            chunk.Data.Vertexmap[index] = vertex;
        }

        private void PaintAtMouse(Vector2 mousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            List<(int x, int z)> targetVertices = new();

            // Vertex tool, use <= not <.
            for (int x = 0; x <= WorldConstants.ChunkSize; x++)
            {
                for (int z = 0; z <= WorldConstants.ChunkSize; z++)
                {
                    Vector3 vertexPosition = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);

                    float intersect = Vector3.Dot(vertexPosition - ray.origin, ray.direction);
                    intersect = Mathf.Max(0, intersect);

                    Vector3 rayPoint = ray.origin + ray.direction * intersect;
                    float distance = Vector3.Distance(vertexPosition, rayPoint);

                    if (distance < brushSize)
                        targetVertices.Add((x, z));
                }
            }

            if (targetVertices.Count > 0)
            {
                Undo.RecordObject(chunk, "Paint Vertex Color(s)");

                foreach (var (x, z) in targetVertices)
                {
                    int index = chunk.Data.GetVertexIndex(x, z);
                    SetSingleColor(index, brushColor);
                }

                RebuildMesh();
            }
        }

        private void RebuildMesh()
        {
            chunk.RebuildMesh();
            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
        }

        private void AdjustBrushSize(Event current)
        {
            if (!current.isScrollWheel)
                return;

            brushSize -= Mathf.Sign(current.delta.y) * 0.5f;
            brushSize = Mathf.Clamp(brushSize, BRUSH_MIN_SIZE, BRUSH_MAX_SIZE);
        }
    }
}