namespace SessionScape.Main.Protocol.Messages
{
    public class RunRequest
    {
        public bool IsSprinting { get; set; }
    }

    public class RunUpdate
    {
        public string EntityId { get; set; } = string.Empty;
        public bool IsSprinting { get; set; }
    }

    public class RunEnergyUpdate
    {
        public double RunEnergy { get; set; }
    }
}