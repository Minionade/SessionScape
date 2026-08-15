using SessionScape.Client.Assets.Scripts.World;
using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapBuilder : MonoBehaviour, IChunkOwner
{
    public Dictionary<(int x, int z), ChunkObject> Chunkmap { get; } = new();

    public bool TryGetChunk(int x, int z, out ChunkObject chunk) => Chunkmap.TryGetValue((x, z), out chunk);

    void OnValidate()
    {
        ValidateRegisteredChunks();
    }

    public void RegisterChunk(int x, int z, ChunkObject chunk)
    {
        Chunkmap[(x, z)] = chunk;
    }

    public void UnregisterChunk(int x, int z, ChunkObject chunk)
    {
        Chunkmap.Remove((x, z));
    }

    public void ValidateRegisteredChunks()
    {
        Chunkmap.Clear();

        foreach (var chunk in GetComponentsInChildren<ChunkObject>())
        {
            if (chunk.Data == null)
                continue;

            (int x, int z) = (chunk.Data.X, chunk.Data.Z);

            RegisterChunk(x, z, chunk);
        }
    }

    public void BuildChunk(int x, int z)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Create new Chunk");

        GameObject chunkGameObject = new($"Chunk ({x}, {z})");

        Undo.RegisterCreatedObjectUndo(chunkGameObject, $"Create new Chunk ({x}, {z})");

        chunkGameObject.transform.SetParent(transform);

        ChunkObject chunkObject = chunkGameObject.AddComponent<ChunkObject>();

        chunkObject.CreateNewData(this, x, z);

        float chunkSize = WorldConstants.ChunkSize * WorldConstants.TileSize;
        chunkGameObject.transform.position = new Vector3(x * chunkSize, 0, z * chunkSize);

        RegisterChunk(x, z, chunkObject);
    }


}
