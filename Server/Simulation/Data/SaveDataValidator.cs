using SessionScape.Main.Protocol.Messages;

namespace SessionScape.Server.Simulation
{
    public static class SaveDataValidator
    {
        private const int MaxStatValue = 99;

        public static bool TryValidate(LoginRequest request, out string rejectReason)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
            {
                rejectReason = "Missing player name.";
                return false;
            }

            if (request.PlayerName.Length > 32)
            {
                rejectReason = "Player name is too long.";
                return false;
            }

            rejectReason = string.Empty;
            return true;
        }
    }
}
