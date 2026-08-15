namespace SessionScape.Main.Protocol
{
    public enum MessageType
    {
        // *9000. Debug*
        EchoRequest = 9001,
        EchoResponse = 9002,

        // *0000. Login*
        LoginRequest = 0001,
        LoginResponse = 0002,

        // *1000. Chat*
        ChatRequest = 1001,
        ChatResponse = 1002,

        // *2000. Entity*
        EntitySnapshotList = 2001,
        EntityMovementUpdate = 2002,
        EntityUpdate = 2003,
        EntityRemoved = 2004,

        // *3000. Asset Streaming*
        ChunkSnapshot = 3001,
        MapSyncRequest = 3002,
        MapUpToDate = 3003,
        MapSyncComplete = 3004,

        // *4000. Interaction*
        InteractionRequest = 4001,
        InteractionResponse = 4002,

        // *5000. GUI
        RunRequest = 5001,
        RunUpdate = 5002,
        RunEnergyUpdate = 5003,
    }
}