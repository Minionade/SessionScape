using SessionScape.Main.World;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SessionScape.Client.Assets.Scripts.World
{
    public class ChunkObject : MonoBehaviour
    {
        public ChunkData Data { get { return _data; } private set { _data = value; } }

        [SerializeField] private ChunkData _data;

        public IChunkOwner Parent { get; private set; }

        private Mesh mesh;
        private Material material;
        public MeshRenderer MeshRenderer { get; private set; }
        public MeshFilter MeshFilter { get; private set; }
        public MeshCollider MeshCollider { get; private set; }

        private void OnEnable()
        {
            RebuildMesh();
        }

        public void CreateNewData(IChunkOwner parent, int x, int z)
        {
            Data = ChunkData.CreateEmpty(x, z, WorldConstants.ChunkSize);
            Parent = parent;

            RebuildMesh();
        }

        public void LoadFromData(IChunkOwner parent, ChunkData data)
        {
            Data = data;
            Parent = parent;

            RebuildMesh();
        }

        public void RebuildMesh()
        {
            if (mesh != null)
            {
                ChunkMeshGenerator.DestroyMesh(ref mesh);
            }

            if (material == null)
            {
                material = ChunkMeshGenerator.GenerateVertexColorMaterial();
            }

            MeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
            MeshFilter = gameObject.GetOrAddComponent<MeshFilter>();
            MeshCollider = gameObject.GetOrAddComponent<MeshCollider>();

            MeshFilter.sharedMesh = null;
            MeshCollider.sharedMesh = null;
            MeshRenderer.sharedMaterial = null;

            MeshRenderer.sharedMaterial = material;
            mesh = ChunkMeshGenerator.GenerateChunkMesh(Data);
            MeshFilter.sharedMesh = mesh;
            MeshCollider.sharedMesh = mesh;
        }

        public bool BakeAssets(out List<string> errors)
        {
            errors = new();
            List<AssetInstanceData> baked = new();
            HashSet<int> usedIds = new();
            int nextId = 1;

            foreach (IWorldAsset asset in GetComponentsInChildren<IWorldAsset>())
            {
                if (!asset.Validate(out string error))
                {
                    errors.Add(error);
                    continue;
                }

                if (asset.InstanceId <= 0 || !usedIds.Add(asset.InstanceId))
                {
                    while (!usedIds.Add(nextId)) nextId++;
                    asset.InstanceId = nextId;
                }

                Transform t = ((Component)asset).transform;
                Vector3 localPosition = t.position - transform.position;

                baked.Add(new AssetInstanceData
                {
                    InstanceId = asset.InstanceId,
                    AssetId = asset.AssetId,
                    AssetLabel = asset.AssetLabel,
                    X = localPosition.x,
                    Y = localPosition.y,
                    Z = localPosition.z,
                    RotationY = t.eulerAngles.y,
                    ScaleX = t.localScale.x,
                    ScaleY = t.localScale.y,
                    ScaleZ = t.localScale.z,
                });
            }

            Data.Assetmap = baked.ToArray();
            return errors.Count == 0; ;
        }

        public void SetColor(int vertexX, int vertexZ, Color32 color)
        {
            int index = Data.GetVertexIndex(vertexX, vertexZ);

            if (index < 0)
                return;

            VertexData vertex = Data.Vertexmap[index];
            vertex.R = color.r;
            vertex.G = color.g;
            vertex.B = color.b;
            vertex.A = color.a;

            Data.Vertexmap[index] = vertex;
        }

        public void SetConnection(int tileX, int tileZ, TileConnections direction, bool connected)
        {
            SetSingleConnection(tileX, tileZ, direction, connected);
            GetTileNeighbor(tileX, tileZ, direction, out int neighborX, out int neighborZ, out TileConnections opposite);

            if (Data.GetTileIndex(neighborX, neighborZ) < 0)
                return;

            SetSingleConnection(neighborX, neighborZ, opposite, connected);
        }

        public void SetAllConnections(int tileX, int tileZ, TileConnections connections, bool connected)
        {
            int index = Data.GetTileIndex(tileX, tileZ);
            if (index < 0)
                return;

            SetSingleConnection(tileX, tileZ, connections, connected);
            SetSingleConnection(tileX, tileZ + 1, TileConnections.south, connections.HasFlag(TileConnections.north));
            SetSingleConnection(tileX, tileZ - 1, TileConnections.north, connections.HasFlag(TileConnections.south));
            SetSingleConnection(tileX + 1, tileZ, TileConnections.west, connections.HasFlag(TileConnections.east));
            SetSingleConnection(tileX - 1, tileZ, TileConnections.east, connections.HasFlag(TileConnections.west));
        }

        private void SetSingleConnection(int tileX, int tileZ, TileConnections direction, bool connected)
        {
            int index = Data.GetTileIndex(tileX, tileZ);

            if (index < 0)
                return;

            TileData tile = Data.Tilemap[index];

            if (connected)
                tile.Connections |= direction;
            else
                tile.Connections &= ~direction;

            Data.Tilemap[index] = tile;
        }

        private void GetTileNeighbor(int tileX, int tileZ, TileConnections direction, out int neighborX, out int neighborZ, out TileConnections opposite)
        {
            neighborX = tileX;
            neighborZ = tileZ;
            opposite = TileConnections.none;

            switch (direction)
            {
                case TileConnections.north:

                    neighborZ++;
                    opposite = TileConnections.south;
                    break;

                case TileConnections.south:

                    neighborZ--;
                    opposite = TileConnections.north;
                    break;

                case TileConnections.east:

                    neighborX++;
                    opposite = TileConnections.west;
                    break;

                case TileConnections.west:

                    neighborX--;
                    opposite = TileConnections.east;
                    break;
            }
        }
    }
}