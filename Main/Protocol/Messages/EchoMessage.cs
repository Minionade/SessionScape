namespace SessionScape.Main.Protocol.Messages
{
    public class EchoRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class EchoResponse
    {
        public string Text { get; set; } = string.Empty;
    }
}