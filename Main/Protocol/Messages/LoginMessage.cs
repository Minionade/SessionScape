namespace SessionScape.Main.Protocol.Messages
{
    public class LoginRequest
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; 
    }

    public class LoginResponse
    {
        public bool Accepted { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string RejectReason { get; set; } = string.Empty;
    }
}
