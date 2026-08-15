namespace SessionScape.Main.World
{
    [System.Serializable]
    public struct AssetInstanceData
    {
        public int InstanceId;
        public int AssetId;

        public string AssetLabel;

        public float X;
        public float Y;
        public float Z;

        public float RotationY;

        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;
    }
}