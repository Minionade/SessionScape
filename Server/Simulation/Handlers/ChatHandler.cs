using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Server.Simulation.World;

namespace SessionScape.Server.Simulation.Handlers
{
    public class ChatHandler : MessageHandler<ChatRequest>
    {
        private readonly WorldState _world;

        public ChatHandler(WorldState world)
        {
            _world = world;
        }

        public override MessageType Type => MessageType.ChatRequest;

        protected override void HandleTyped(ChatRequest data, PendingAction action, long currentTick)
        {
            Console.WriteLine($"[Tick {currentTick}] {action.Player.Name}: {data.Text}");

            var response = MessageEnvelope.Create(MessageType.ChatResponse, action.Envelope.Sequence, currentTick,
                new ChatResponse { PlayerName = action.Player.Name, Text = data.Text });

            _world.BroadcastToAll(response);
        }
    }
}
