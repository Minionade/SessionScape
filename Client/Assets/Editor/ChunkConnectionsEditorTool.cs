using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace SessionScape.Client.Assets.Editor
{
    [EditorTool("Chunk Connections Painter", typeof(ChunkObject))]
    public class ChunkConnectionsEditorTool : EditorTool
    {
        private struct TileConnectionTarget
        {
            public int x, z;
            public TileConnections direction;

            public bool IsValid => direction != TileConnections.none;
            public override bool Equals(object obj)
            {
                if (obj is not TileConnectionTarget other)
                    return false;

                return x == other.x && z == other.z && direction == other.direction;
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine(x, z, direction);
            }
        }

        const float CONNECTION_Y_OFFSET = 0.035f;
        const float CONNECTION_LINE_WIDTH = 0.3f;
        const float EDGE_SELECTION_THRESHOLD = 0.3f;

        private ChunkObject chunk;
        private MeshCollider chunkCollider;

        private bool isPainting;
        private bool paintState;

        private TileConnections dragDirection;
        private Vector2 dragStartMousePosition;

        private TileConnectionTarget lastPaintedTarget;
        private TileConnectionTarget cachedHoverTarget;
        private bool hasCachedHoverTarget;

        private bool isMarqueeSelecting;
        private Vector2Int marqueeStart;
        private Vector2Int marqueeCurrent;

        private readonly List<Vector3> connectionVertices = new();
        private readonly List<Color> connectionColors = new();
        private readonly List<int> connectionIndices = new();

        private readonly List<Vector3> brushVertices = new(2);
        private readonly Color[] brushColors = { Color.yellow, Color.yellow };
        private readonly int[] brushIndices = { 0, 1 };

        private Mesh previewGridMesh;
        private Material lineMaterial;

        private Mesh connectionMesh;
        private Material connectionMaterial;

        private bool connectionMeshDirty = true;

        private Mesh brushMesh;
        private Material brushMaterial;

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("d_ToggleUVOverlay");

        public override void OnToolGUI(EditorWindow window)
        {
            chunk = target as ChunkObject;

            if (chunk == null)
            {
                chunkCollider = null;
                return;
            }

            if (chunk.Data == null)
                return;

            if (chunkCollider == null)
                chunk.TryGetComponent(out chunkCollider);

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            RenderConnectionMesh();
            HandleInput();
            RenderBrush();
            RenderMarqueeSelection();
        }

        public override void OnActivated()
        {
            isPainting = false;
            isMarqueeSelecting = false;

            hasCachedHoverTarget = false;
            connectionMeshDirty = true;
        }

        public override void OnWillBeDeactivated()
        {
            isPainting = false;
            isMarqueeSelecting = false;

            hasCachedHoverTarget = false;

            ChunkMeshGenerator.DestroyMesh(ref previewGridMesh);
            ChunkMeshGenerator.DestroyMesh(ref connectionMesh);
            ChunkMeshGenerator.DestroyMesh(ref brushMesh);

            ChunkMeshGenerator.DestroyMaterial(ref lineMaterial);
            ChunkMeshGenerator.DestroyMaterial(ref connectionMaterial);
            ChunkMeshGenerator.DestroyMaterial(ref brushMaterial);

            connectionVertices.Clear();
            connectionColors.Clear();
            connectionIndices.Clear();

            brushVertices.Clear();

            chunkCollider = null;
        }

        private void RenderConnectionMesh()
        {
            if (connectionMesh == null)
            {
                connectionMesh = new Mesh
                {
                    name = "Tile Connections Preview Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };

                connectionMesh.MarkDynamic();
                connectionMeshDirty = true;
            }

            if (connectionMaterial == null)
            {
                connectionMaterial = ChunkMeshGenerator.GenerateLineMaterial();
            }

            if (connectionMeshDirty)
            {
                RebuildConnectionMesh();
            }

            Graphics.DrawMesh(connectionMesh, Matrix4x4.identity, connectionMaterial, 0);
        }

        private void RenderBrush()
        {
            if (isMarqueeSelecting)
            {
                hasCachedHoverTarget = false;
                return;
            }

            Event current = Event.current;

            if (!TryGetConnectionUnderMouse(current.mousePosition, out TileConnectionTarget target))
            {
                hasCachedHoverTarget = false;
                ChunkMeshGenerator.DestroyMesh(ref brushMesh);
                return;
            }

            if (brushMesh == null)
            {
                brushMesh = new Mesh
                {
                    name = "Connection Brush Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (brushMaterial == null)
                brushMaterial = ChunkMeshGenerator.GenerateLineMaterial();

            if (!hasCachedHoverTarget || !cachedHoverTarget.Equals(target))
            {
                cachedHoverTarget = target;
                hasCachedHoverTarget = true;

                GetConnectionEdge(target.x, target.z, target.direction, out Vector3 start, out Vector3 end);

                start.y += CONNECTION_Y_OFFSET + 0.01f;
                end.y += CONNECTION_Y_OFFSET + 0.01f;

                brushVertices.Clear();

                brushVertices.Add(start);
                brushVertices.Add(end);

                brushMesh.Clear();

                brushMesh.SetVertices(brushVertices);
                brushMesh.SetColors(brushColors);
                brushMesh.SetIndices(brushIndices, MeshTopology.Lines, 0);

                brushMesh.RecalculateBounds();
            }

            Graphics.DrawMesh(brushMesh, Matrix4x4.identity, brushMaterial, 0);
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
            Vector3 maxWorld = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, maxX + 1, maxZ + 1);

            Vector3 center = (minWorld + maxWorld) * 0.5f;

            float sizeX = maxWorld.x - minWorld.x;
            float sizeZ = maxWorld.z - minWorld.z;

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

            if (current.button == 1 && isMarqueeSelecting)
            {
                isMarqueeSelecting = false;
                current.Use();
                return;
            }

            if (current.button != 0)
                return;

            if (!TryGetConnectionUnderMouse(current.mousePosition, out TileConnectionTarget target))
                return;

            int index = chunk.Data.GetTileIndex(target.x, target.z);

            if (index < 0 || index >= chunk.Data.Tilemap.Length)
                return;

            paintState = !chunk.Data.Tilemap[index].Connections.HasFlag(target.direction);

            if (current.shift)
            {
                isMarqueeSelecting = true;

                marqueeStart = new Vector2Int(target.x, target.z);
                marqueeCurrent = marqueeStart;

                current.Use();
                return;
            }

            isPainting = true;

            dragDirection = target.direction;

            dragStartMousePosition = current.mousePosition;

            lastPaintedTarget = default;

            PaintConnection(target);

            current.Use();
        }

        private void HandleMouseDrag(Event current)
        {
            if (current.type != EventType.MouseDrag)
                return;

            TileConnectionTarget target;

            if (isMarqueeSelecting)
            {
                if (TryGetConnectionUnderMouse(current.mousePosition, out target))
                {
                    marqueeCurrent = new Vector2Int(target.x, target.z);

                    current.Use();
                }

                return;
            }

            if (!isPainting || current.button != 0)
                return;

            if (!TryGetConnectionUnderMouse(current.mousePosition, out target))
                return;

            UpdateDragDirection(current.mousePosition);

            if (IsSameConnectionAxis(target.direction, dragDirection))
                PaintConnection(target);

            current.Use();
        }

        private void HandleMouseUp(Event current)
        {
            if (current.type != EventType.MouseUp)
                return;

            if (isMarqueeSelecting && current.button == 0)
            {
                isMarqueeSelecting = false;

                PaintMarquee(marqueeStart, marqueeCurrent);

                current.Use();

                return;
            }

            if (isPainting && current.button == 0)
            {
                isPainting = false;

                dragDirection = TileConnections.none;

                lastPaintedTarget = default;

                current.Use();
            }
        }

        private void UpdateDragDirection(Vector2 currentMousePosition)
        {
            Vector3 startWorld;
            Vector3 currentWorld;

            if (!TryGetMouseWorldPosition(dragStartMousePosition, out startWorld))
                return;

            if (!TryGetMouseWorldPosition(currentMousePosition, out currentWorld))
                return;

            Vector3 dragDelta = currentWorld - startWorld;

            if (dragDelta.sqrMagnitude < 0.01f)
                return;

            bool dragIsWorldX = Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.z);

            bool currentDirectionIsWorldX =
                dragDirection == TileConnections.north ||
                dragDirection == TileConnections.south;

            if (dragIsWorldX == currentDirectionIsWorldX)
                return;

            dragDirection = currentDirectionIsWorldX
                ? TileConnections.east
                : TileConnections.north;
        }

        private bool TryGetMouseWorldPosition(Vector2 mousePosition, out Vector3 worldPosition)
        {
            worldPosition = default;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (!plane.Raycast(ray, out float distance))
                return false;

            worldPosition = ray.GetPoint(distance);
            return true;
        }

        private void RebuildConnectionMesh()
        {
            connectionVertices.Clear();
            connectionColors.Clear();
            connectionIndices.Clear();

            int chunkSize = WorldConstants.ChunkSize;
            int expectedVertexCount = chunkSize * chunkSize * 8;

            if (connectionVertices.Capacity < expectedVertexCount) 
                connectionVertices.Capacity = expectedVertexCount;

            if (connectionColors.Capacity < expectedVertexCount)
                connectionColors.Capacity = expectedVertexCount;

            if (connectionIndices.Capacity < expectedVertexCount)
                connectionIndices.Capacity = expectedVertexCount;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int index = chunk.Data.GetTileIndex(x, z);

                    if (index < 0 || index >= chunk.Data.Tilemap.Length)
                        continue;

                    TileConnections connection = chunk.Data.Tilemap[index].Connections;

                    AddConnectionEdge(connectionVertices, connectionColors, connectionIndices, x, z,
                        TileConnections.north, connection.HasFlag(TileConnections.north));

                    AddConnectionEdge(connectionVertices, connectionColors, connectionIndices, x, z,
                        TileConnections.south, connection.HasFlag(TileConnections.south));

                    AddConnectionEdge(connectionVertices, connectionColors, connectionIndices, x, z,
                        TileConnections.east, connection.HasFlag(TileConnections.east));

                    AddConnectionEdge(connectionVertices, connectionColors, connectionIndices, x, z,
                        TileConnections.west, connection.HasFlag(TileConnections.west));
                }
            }

            connectionMesh.Clear();

            connectionMesh.SetVertices(connectionVertices);
            connectionMesh.SetColors(connectionColors);
            connectionMesh.SetIndices(connectionIndices, MeshTopology.Triangles, 0);

            connectionMesh.RecalculateBounds();

            connectionMeshDirty = false;
        }

        private void PaintConnection(TileConnectionTarget target)
        {
            if (!target.IsValid)
                return;

            if (target.Equals(lastPaintedTarget))
                return;

            lastPaintedTarget = target;

            Undo.RecordObject(chunk, "Paint Tile Connection");

            SetConnection(target.x, target.z, target.direction, paintState);

            connectionMeshDirty = true;

            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
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

            TileConnections allConnections = TileConnections.north | TileConnections.south | TileConnections.east | TileConnections.west;
            Undo.RecordObject(chunk, "Paint Connections");

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    SetAllConnections(x, z, paintState ? allConnections : TileConnections.none);
                }
            }

            connectionMeshDirty = true;
            
            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
        }

        private void SetConnection(int x, int z, TileConnections direction, bool connected)
        {
            SetSingleConnection(x, z, direction, connected);

            GetNeighbor(x, z, direction, out int neighborX, out int neighborZ, out TileConnections opposite);

            if (chunk.Data.GetTileIndex(neighborX, neighborZ) >= 0)
                SetSingleConnection(neighborX, neighborZ, opposite, connected);
        }

        private void SetAllConnections(int x, int z, TileConnections connections)
        {
            int index = chunk.Data.GetTileIndex(x, z);
            if (index < 0)
                return;

            TileData tile = chunk.Data.Tilemap[index];
            tile.Connections = connections;
            chunk.Data.Tilemap[index] = tile;

            SetSingleConnection(x, z + 1, TileConnections.south, connections.HasFlag(TileConnections.north));

            SetSingleConnection(x, z - 1, TileConnections.north, connections.HasFlag(TileConnections.south));

            SetSingleConnection(x + 1, z, TileConnections.west, connections.HasFlag(TileConnections.east));

            SetSingleConnection(x - 1, z, TileConnections.east, connections.HasFlag(TileConnections.west));
        }

        private void SetSingleConnection(int x, int z, TileConnections direction, bool connected)
        {
            int index = chunk.Data.GetTileIndex(x, z);
            if (index < 0)
                return;

            TileData tile = chunk.Data.Tilemap[index];

            if (connected)
                tile.Connections |= direction;
            else
                tile.Connections &= ~direction;

            chunk.Data.Tilemap[index] = tile;
        }

        private void GetNeighbor(int x, int z, TileConnections direction, out int neighborX, out int neighborZ, out TileConnections opposite)
        {
            neighborX = x; neighborZ = z;
            opposite = TileCoordinateMath.GetOpposite(direction);

            if (direction == TileConnections.north)
                neighborZ++;
            else if (direction == TileConnections.south)
                neighborZ--;
            else if (direction == TileConnections.east)
                neighborX++;
            else if (direction == TileConnections.west)
                neighborX--;
            else
                opposite = TileConnections.none;
        }

        private bool TryGetConnectionUnderMouse(Vector2 mousePosition, out TileConnectionTarget target)
        {
            target = default;

            if (chunkCollider == null)
                return false;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (!chunkCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                return false;

            int worldX = Mathf.FloorToInt(hit.point.x + 0.5f);
            int worldZ = Mathf.FloorToInt(hit.point.z + 0.5f);

            TileCoordinateMath.WorldToLocalTile(worldX, worldZ, out int localX, out int localZ);

            if (!TryGetClosestConnection(localX, localZ, hit.point, out TileConnections direction))
            {
                return false;
            }

            target = new TileConnectionTarget
            {
                x = localX,
                z = localZ,
                direction = direction
            };

            return true;
        }

        private bool TryGetClosestConnection(
            int x,
            int z,
            Vector3 point,
            out TileConnections direction)
        {
            direction = TileConnections.none;

            Vector3 northStart = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z + 1);
            Vector3 northEnd = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x + 1, z + 1);

            Vector3 southStart = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);
            Vector3 southEnd = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x + 1, z);

            Vector3 eastStart = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x + 1, z);
            Vector3 eastEnd = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x + 1, z + 1);

            Vector3 westStart = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z);
            Vector3 westEnd = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, x, z + 1);

            float northDistance = DistanceToLineSegmentSqr(point, northStart, northEnd);
            float southDistance = DistanceToLineSegmentSqr(point, southStart, southEnd);
            float eastDistance = DistanceToLineSegmentSqr(point, eastStart, eastEnd);
            float westDistance = DistanceToLineSegmentSqr(point, westStart, westEnd);

            float closestDistance = Mathf.Min(northDistance, southDistance, eastDistance, westDistance);

            float threshold = EDGE_SELECTION_THRESHOLD * WorldConstants.TileSize;

            if (closestDistance > threshold * threshold)
                return false;

            if (closestDistance == northDistance)
                direction = TileConnections.north;
            else if (closestDistance == southDistance)
                direction = TileConnections.south;
            else if (closestDistance == eastDistance)
                direction = TileConnections.east;
            else
                direction = TileConnections.west;

            return true;
        }

        private float DistanceToLineSegmentSqr(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;

            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr < Mathf.Epsilon)
                return (point - start).magnitude;

            float t = Vector3.Dot(point - start, segment) / segmentLengthSqr;
            t = Mathf.Clamp01(t);

            Vector3 closestPoint = start + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }

        private bool IsSameConnectionAxis(TileConnections a, TileConnections b)
        {
            bool aIsHorizontal = a == TileConnections.north || a == TileConnections.south;
            bool bIsHorizontal = b == TileConnections.north || b == TileConnections.south;
            return aIsHorizontal == bIsHorizontal;
        }

        private void AddConnectionEdge(List<Vector3> vertices, List<Color> colors, List<int> indices, int x, int z, TileConnections direction, bool connected)
        {
            GetConnectionEdge(x, z, direction, out Vector3 start, out Vector3 end);

            start.y += CONNECTION_Y_OFFSET;
            end.y += CONNECTION_Y_OFFSET;

            float halfWidth = CONNECTION_LINE_WIDTH * 0.5f;

            Vector3 edgeDirection = end - start;
            Vector3 horizontalDirection = new Vector3(edgeDirection.x, 0f, edgeDirection.z).normalized;
            Vector3 perpendicular = new Vector3(-horizontalDirection.z, 0f, horizontalDirection.x) * halfWidth;

            int vertexIndex = vertices.Count;

            vertices.Add(start - perpendicular);
            vertices.Add(start + perpendicular);
            vertices.Add(end + perpendicular);
            vertices.Add(end - perpendicular);

            Color color = connected ? Color.green : Color.red;

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            indices.Add(vertexIndex);
            indices.Add(vertexIndex + 1);
            indices.Add(vertexIndex + 2);

            indices.Add(vertexIndex);
            indices.Add(vertexIndex + 2);
            indices.Add(vertexIndex + 3);
        }

        private void GetConnectionEdge(int x, int z, TileConnections direction, out Vector3 start, out Vector3 end)
        {
            int localX, localZ;
            switch (direction)
            {
                case TileConnections.north:

                    TileCoordinateMath.WorldToLocalVertex(x, z + 1, out localX, out localZ);
                    start = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    TileCoordinateMath.WorldToLocalVertex(x + 1, z + 1, out localX, out localZ);
                    end = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    break;
                case TileConnections.south:

                    TileCoordinateMath.WorldToLocalVertex(x, z, out localX, out localZ);
                    start = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    TileCoordinateMath.WorldToLocalVertex(x + 1, z, out localX, out localZ);
                    end = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    break;
                case TileConnections.east:

                    TileCoordinateMath.WorldToLocalVertex(x + 1, z, out localX, out localZ);
                    start = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    TileCoordinateMath.WorldToLocalVertex(x + 1, z + 1, out localX, out localZ);
                    end = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    break;
                case TileConnections.west:

                    TileCoordinateMath.WorldToLocalVertex(x, z, out localX, out localZ);
                    start = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    TileCoordinateMath.WorldToLocalVertex(x, z + 1, out localX, out localZ);
                    end = ChunkMathHelper.LocalVertexToWorldPosition(chunk.Data, localX, localZ);

                    break;
                default:
                    start = end = Vector3.zero;
                    break;
            }
        }
    }
}