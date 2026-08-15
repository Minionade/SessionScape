public interface IWorldAsset
{
    int AssetId { get; }
    string AssetLabel { get; }
    int InstanceId { get; set; }
    bool Validate(out string error);
}