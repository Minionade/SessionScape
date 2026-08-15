namespace SessionScape.Client.Assets.Scripts.World
{
    public interface IChunkOwner
    {
        bool TryGetChunk(int x, int z, out ChunkObject chunk);
    }

}