using SessionScape.Main.Protocol;
using System.Net.Sockets;

namespace SessionScape.Server.Simulation
{
    public class PendingAction
    {
        public MessageEnvelope Envelope { get; set; }
        public NetworkStream ResponseStream { get; set; }
        public PlayerState Player { get; }

        public PendingAction(MessageEnvelope envelope, NetworkStream responseStream, PlayerState player)
        {
            Envelope = envelope;
            ResponseStream = responseStream;
            Player = player;
        }
    }
}