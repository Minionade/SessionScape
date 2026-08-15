using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SessionScape/Asset Registry")]
public class AssetRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int AssetId;
        public string AssetLabel;
        public GameObject Prefab;
    }

    public List<Entry> Entries;

    public bool TryGetPrefab(int assetId, out GameObject prefab)
    {
        var entry = Entries.Find(e => e.AssetId == assetId);
        prefab = entry?.Prefab;
        return entry != null;
    }
}
