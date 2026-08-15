namespace SessionScape.Main.Protocol.Messages
{
    public enum InteractionTargetType
    {
        Tile,
        InventoryItem,
        WorldEntity,
        WorldObject
    }

    public enum InteractionVerb
    {
        WalkHere,
        Use,
        Wear,
        Attack,
        TalkTo,
        Examine,
        Consume,
        Drop
    }

    public class InteractionRequest
    {
        public InteractionTargetType TargetType { get; set; }
        public InteractionVerb Verb { get; set; }

        public int? TargetX { get; set; }
        public int? TargetZ { get; set; }
        public int? SlotIndex { get; set; }
        public string EntityId { get; set; }

        public int? SecondarySlotIndex { get; set; }
        public string SecondaryEntityId { get; set; }
    }

    public class InteractionResponse
    {
        public bool Accepted { get; set; }
        public string RejectReason { get; set; } = string.Empty;
    }
}