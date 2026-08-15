namespace SessionScape.Main.Items
{
    [System.Serializable]
    public class ItemDefinition
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Stackable { get; set; }
        public int MaxStack { get; set; } = 1;
    }
}