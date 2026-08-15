using SessionScape.Main.World;
using UnityEngine;

public class WorldPropAsset : MonoBehaviour, IWorldAsset
{
    [SerializeField] private int assetId;
    [SerializeField] private string assetLabel;
    [SerializeField] private int instanceId;

    public int AssetId => assetId;
    public string AssetLabel => assetLabel;
    public int InstanceId { get => instanceId; set => instanceId = value; }

    public bool Validate(out string error)
    {
        if (assetId < 0) { error = "AssetId not set"; return false; }
        if (string.IsNullOrEmpty(assetLabel)) { error = "AssetLabel not set"; return false; }

        error = null;
        return true;
    }
}
