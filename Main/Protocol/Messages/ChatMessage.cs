namespace SessionScape.Main.Protocol.Messages
{
    public class ChatRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}