using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEngine;

namespace SessionScape.Client.Assets.Scripts.World
{
    public static class ChunkMeshGenerator
    {
        public static Mesh GenerateChunkMesh(ChunkData data)
        {
            Mesh mesh = new Mesh
            {
                name = "Chunk Mesh",
                vertices = GetChunkVertices(data),
                triangles = GetChunkTriangles(),
                colors = GetChunkColors(data),
                normals = GetChunkNormals(data)
            };

            GetChunkCornerColors(
                data,
                out List<Vector4> corner00,
                out List<Vector4> corner10,
                out List<Vector4> corner01,
                out List<Vector4> corner11
            );

            mesh.SetUVs(0, corner00);
            mesh.SetUVs(1, corner10);
            mesh.SetUVs(2, corner01);
            mesh.SetUVs(3, corner11);
            mesh.SetUVs(4, GetChunkTileUVs());

            mesh.RecalculateBounds();

            return mesh;
        }

        public static Mesh GenerateSingleTileMesh(ChunkData data, int x, int z, Color color)
        {
            Mesh mesh = new Mesh
            {
                name = "Tile Mesh",
                vertices = GetTileVertices(data, x, z),
                triangles = GetTileTriangles(),
                colors = GetTileColors(color)
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static Mesh GenerateGridMesh(string name, ChunkData data)
        {
            const float GRID_Y_OFFSET = 0.1f;

            List<Vector3> vertices = new();
            List<int> lines = new();

            int vertexAmount = WorldConstants.ChunkSize + 1;

            for (int z = 0; z < vertexAmount; z++)
            {
                for (int x = 0; x < vertexAmount; x++)
                {
                    Vector3 vertexPosition = new Vector3
                    {
                        x = (x * WorldConstants.TileSize) - (WorldConstants.ChunkSize * WorldConstants.TileSize * 0.5f),
                        y = data.GetVertexHeight(x, z) + GRID_Y_OFFSET,
                        z = (z * WorldConstants.TileSize) - (WorldConstants.ChunkSize * WorldConstants.TileSize * 0.5f)
                    };

                    vertices.Add(vertexPosition);

                    int currentIndex = data.GetVertexIndex(x, z);

                    if (x < WorldConstants.ChunkSize)
                    {
                        int rightIndex = data.GetVertexIndex(x + 1, z);
                        lines.Add(currentIndex);
                        lines.Add(rightIndex);
                    }

                    if (z < WorldConstants.ChunkSize)
                    {
                        int northIndex = data.GetVertexIndex(x, z + 1);
                        lines.Add(currentIndex);
                        lines.Add(northIndex);
                    }
                }
            }

            Mesh mesh = new Mesh
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };

            mesh.SetVertices(vertices);
            mesh.SetIndices(lines, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

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

        public static Material GenerateLineMaterial()
        {
            Material lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            lineMaterial.SetPass(0);

            return lineMaterial;
        }

        public static Material GenerateVertexColorMaterial()
        {
            return new Material(Shader.Find("Custom/OSRSTerrain"));
        }

        public static Material GeneratePriorityVertexColorMaterial()
        {
            Material mat = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            return mat;
        }

        private static Vector3[] GetChunkVertices(ChunkData data)
        {
            int chunkSize = WorldConstants.ChunkSize;
            float tileSize = WorldConstants.TileSize;
            float offset = chunkSize * tileSize * 0.5f;

            Vector3[] vertices = new Vector3[chunkSize * chunkSize * 6];

            int index = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float x0 = (x * tileSize) - offset;
                    float x1 = ((x + 1) * tileSize) - offset;
                    float z0 = (z * tileSize) - offset;
                    float z1 = ((z + 1) * tileSize) - offset;

                    Vector3 v00 = new(x0, data.GetVertexHeight(x, z), z0);
                    Vector3 v10 = new(x1, data.GetVertexHeight(x + 1, z), z0);
                    Vector3 v01 = new(x0, data.GetVertexHeight(x, z + 1), z1);
                    Vector3 v11 = new(x1, data.GetVertexHeight(x + 1, z + 1), z1);

                    vertices[index++] = v00;
                    vertices[index++] = v01;
                    vertices[index++] = v10;

                    vertices[index++] = v10;
                    vertices[index++] = v01;
                    vertices[index++] = v11;
                }
            }

            return vertices;
        }

        private static int[] GetChunkTriangles()
        {
            int chunkSize = WorldConstants.ChunkSize;
            int[] triangles = new int[chunkSize * chunkSize * 6];

            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            return triangles;
        }

        private static Color[] GetChunkColors(ChunkData data)
        {
            int chunkSize = WorldConstants.ChunkSize;
            Color[] colors = new Color[chunkSize * chunkSize * 6];

            int index = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    data.GetVertexColor(x, z, out var b00);
                    data.GetVertexColor(x + 1, z, out var b10);
                    data.GetVertexColor(x, z + 1, out var b01);
                    data.GetVertexColor(x + 1, z + 1, out var b11);

                    Color c00 = ConvertToColor(b00);
                    Color c10 = ConvertToColor(b10);
                    Color c01 = ConvertToColor(b01);
                    Color c11 = ConvertToColor(b11);

                    colors[index++] = c00;
                    colors[index++] = c01;
                    colors[index++] = c10;

                    colors[index++] = c10;
                    colors[index++] = c01;
                    colors[index++] = c11;
                }
            }

            return colors;
        }

        private static void GetChunkCornerColors(
            ChunkData data,
            out List<Vector4> corner00,
            out List<Vector4> corner10,
            out List<Vector4> corner01,
            out List<Vector4> corner11)
        {
            int chunkSize = WorldConstants.ChunkSize;
            int vertexCount = chunkSize * chunkSize * 6;

            corner00 = new List<Vector4>(vertexCount);
            corner10 = new List<Vector4>(vertexCount);
            corner01 = new List<Vector4>(vertexCount);
            corner11 = new List<Vector4>(vertexCount);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    data.GetVertexColor(x, z, out var b00);
                    data.GetVertexColor(x + 1, z, out var b10);
                    data.GetVertexColor(x, z + 1, out var b01);
                    data.GetVertexColor(x + 1, z + 1, out var b11);

                    Vector4 c00 = ConvertToVector4(b00);
                    Vector4 c10 = ConvertToVector4(b10);
                    Vector4 c01 = ConvertToVector4(b01);
                    Vector4 c11 = ConvertToVector4(b11);

                    for (int i = 0; i < 6; i++)
                    {
                        corner00.Add(c00);
                        corner10.Add(c10);
                        corner01.Add(c01);
                        corner11.Add(c11);
                    }
                }
            }
        }

        private static List<Vector2> GetChunkTileUVs()
        {
            int chunkSize = WorldConstants.ChunkSize;
            int vertexCount = chunkSize * chunkSize * 6;

            List<Vector2> uvs = new List<Vector2>(vertexCount);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 0));

                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                }
            }

            return uvs;
        }

        private static Vector4 ConvertToVector4((byte r, byte g, byte b, byte a) byteColor)
        {
            return new Vector4(
                byteColor.r / 255f,
                byteColor.g / 255f,
                byteColor.b / 255f,
                byteColor.a / 255f
            );
        }

        private static Vector3[] GetChunkNormals(ChunkData data)
        {
            int chunkSize = WorldConstants.ChunkSize;
            float tileSize = WorldConstants.TileSize;
            float offset = chunkSize * tileSize * 0.5f;

            Vector3[] normals = new Vector3[chunkSize * chunkSize * 6];

            int index = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float x0 = (x * tileSize) - offset;
                    float x1 = ((x + 1) * tileSize) - offset;
                    float z0 = (z * tileSize) - offset;
                    float z1 = ((z + 1) * tileSize) - offset;

                    Vector3 v00 = new(x0, data.GetVertexHeight(x, z), z0);
                    Vector3 v10 = new(x1, data.GetVertexHeight(x + 1, z), z0);
                    Vector3 v01 = new(x0, data.GetVertexHeight(x, z + 1), z1);
                    Vector3 v11 = new(x1, data.GetVertexHeight(x + 1, z + 1), z1);

                    Vector3 xDirection = ((v10 - v00) + (v11 - v01)) * 0.5f;
                    Vector3 zDirection = ((v01 - v00) + (v11 - v10)) * 0.5f;

                    Vector3 normal = Vector3.Cross(zDirection, xDirection).normalized;

                    normals[index++] = normal;
                    normals[index++] = normal;
                    normals[index++] = normal;

                    normals[index++] = normal;
                    normals[index++] = normal;
                    normals[index++] = normal;
                }
            }

            return normals;
        }

        public static Vector3[] GetTileVertices(ChunkData data, int x, int z)
        {
            Vector3 v00 = ChunkMathHelper.LocalVertexToWorldPosition(data, x, z);
            Vector3 v10 = ChunkMathHelper.LocalVertexToWorldPosition(data, x + 1, z);
            Vector3 v01 = ChunkMathHelper.LocalVertexToWorldPosition(data, x, z + 1);
            Vector3 v11 = ChunkMathHelper.LocalVertexToWorldPosition(data, x + 1, z + 1);

            v00.y += 0.03f;
            v10.y += 0.03f;
            v01.y += 0.03f;
            v11.y += 0.03f;

            return new Vector3[]
            {
                v00, v10, v01, v11
            };
        }

        public static int[] GetTileTriangles()
        {
            return new int[]
            {
                0, 2, 1,
                1, 2, 3
            };
        }

        public static Color[] GetTileColors(Color color)
        {
            return new Color[]
            {
                color,
                color,
                color,
                color
            };
        }

        private static Color ConvertToColor((byte r, byte g, byte b, byte a) byteColor)
        {
            return new Color32
            {
                r = byteColor.r,
                g = byteColor.g,
                b = byteColor.b,
                a = byteColor.a
            };
        }
    }
}