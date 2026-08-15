using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SessionScape.Client.Assets.Editor
{
    [EditorTool("Map Builder", typeof(MapBuilder))]
    public class MapBuilderEditorTool : EditorTool
    {
        const float GIZMO_SIZE = 0.5f;
        const string DEFAULT_MAP_ID = "DefaultMap";
        const int FORMAT_VERSION = 1;
        const int MAP_VERSION = 1;

        private MapBuilder map;
        private Dictionary<(int x, int z), Vector3> gizmoPositions = new();

        string mapId = DEFAULT_MAP_ID;
        string outputDirectory = "Maps";

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("d_MoveTool on");

        public override void OnToolGUI(EditorWindow window)
        {
            map = target as MapBuilder;

            if (map == null)
                return;

            map.ValidateRegisteredChunks();
            RenderChunkBuilderButtons();
            RenderSaveButton();
        }

        public override void OnActivated()
        {
            map = target as MapBuilder;

            if (map == null)
                return;  
        }

        private void RenderChunkBuilderButtons()
        {
            var chunks = map.Chunkmap;

            if (chunks.Count == 0 || !chunks.ContainsKey((0, 0)))
            {
                RenderChunkBuilderGizmo(0, 0, Vector3.up);
                return;
            }

            foreach (var (x, z, direction) in GetAdjacentEmptyPositions(chunks))
            {
                RenderChunkBuilderGizmo(x, z, direction);
            }
        }

        private void RenderSaveButton()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 250, 150), EditorStyles.helpBox);

            GUILayout.Label("Map Save Data", EditorStyles.boldLabel);

            mapId = EditorGUILayout.TextField("Map Identifier", mapId);

            outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);

            GUILayout.Space(5);

            if (GUILayout.Button("Save Map"))
            {
                SaveMap();
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void SaveMap()
        {
            if (map == null)
            {
                Debug.LogError("Error saving map: MapBuilder is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                Debug.LogError("Error saving map: Map ID cannot be empty.");
                return;
            }

            if (map.Chunkmap.Count == 0)
            {
                Debug.LogError("Error saving map: No chunks found.");
                return;
            }

            try
            {
                MapFileExporter.Export(map, mapId, FORMAT_VERSION, MAP_VERSION, Path.Combine(Application.dataPath, outputDirectory));

                AssetDatabase.Refresh();

                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(map.gameObject.scene);

                Debug.Log($"Map '{mapId}' saved successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save map '{mapId}'.\n" + ex);
            }
        }

        private void RenderChunkBuilderGizmo(int x, int z, Vector3 direction)
        {
            float chunkWorldSize = WorldConstants.ChunkSize * WorldConstants.TileSize;

            Vector3 position = new(x * chunkWorldSize, 0, z * chunkWorldSize);
            gizmoPositions[(x, z)] = position;

            Handles.color = Color.darkRed;
            
            float handleSize = HandleUtility.GetHandleSize(position) * GIZMO_SIZE;
            Quaternion facingDirection = Quaternion.LookRotation(direction, Vector3.up);

            if (Handles.Button(position, facingDirection, handleSize, handleSize, Handles.ArrowHandleCap))
            {
                map.BuildChunk(x, z);
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            }

            Handles.color = Color.yellow;
            Handles.DrawWireCube(position, Vector3.one * chunkWorldSize);
            Handles.Label(position + Vector3.up * 0.7f, $"({x}, {z})");
        }

        private List<(int x, int z, Vector3 direction)> GetAdjacentEmptyPositions(Dictionary<(int x, int z), ChunkObject> chunks)
        {
            var emptyPositions = new List<(int x, int z, Vector3 direction)>();
            int minX = int.MaxValue;
            int maxX = int.MinValue;

            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            foreach (var (chunkCoord, _) in chunks)
            {
                minX = Mathf.Min(minX, chunkCoord.x);
                maxX = Mathf.Max(maxX, chunkCoord.x);

                minZ = Mathf.Min(minZ, chunkCoord.z);
                maxZ = Mathf.Max(maxZ, chunkCoord.z);
            }

            for (int x = minX - 1; x <= maxX + 1; x++)
            {
                for (int z = minZ - 1;  z <= maxZ + 1; z++)
                {
                    if (chunks.ContainsKey((x, z)))
                        continue;

                    if (IsAdjacentToChunk(x, z, chunks, out Vector3 direction))
                        emptyPositions.Add((x, z, direction));
                }
            }

            return emptyPositions;
        }

        private bool IsAdjacentToChunk(int x, int z, Dictionary<(int x, int z), ChunkObject> chunks, out Vector3 direction)
        {
            if (chunks.ContainsKey((x + 1, z)))
            {
                direction = Vector3.left;
                return true;
            }
            else if (chunks.ContainsKey((x - 1, z)))
            {
                direction = Vector3.right;
                return true;
            }
            else if (chunks.ContainsKey((x, z + 1)))
            {
                direction = Vector3.back;
                return true;
            }
            else if (chunks.ContainsKey((x, z - 1)))
            {
                direction = Vector3.forward;
                return true;
            }

            direction = Vector3.zero;
            return false;
        }
    }
}