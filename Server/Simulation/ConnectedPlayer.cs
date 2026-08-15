using SessionScape.Server.Simulation.World;
using System.Net.Sockets;

namespace SessionScape.Server.Simulation
{
    public class ConnectedPlayer
    {
        public PlayerState State { get; }
        public NetworkStream Stream { get; }

        public Dictionary<(int x, int z), string> LoadedChunks { get; } = new();
        public ConnectedPlayer(PlayerState state, NetworkStream stream)
        {
            State = state;
            Stream = stream;
        }
    }
}
