using Server.Simulation;
using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Server.Simulation.World;

namespace SessionScape.Server.Simulation.Handlers
{
    public class SprintHandler : MessageHandler<RunRequest>
    {
        private readonly WorldState _world;

        public SprintHandler(WorldState world)
        {
            _world = world;
        }

        public override MessageType Type => MessageType.RunRequest;

        protected override void HandleTyped(RunRequest data, PendingAction action, long currentTick)
        {
            bool result = action.Player.Movement.TrySetRun(data.IsSprinting);

            var update = MessageEnvelope.Create(MessageType.RunUpdate, action.Envelope.Sequence, currentTick,
                new RunUpdate
                {
                    EntityId = action.Player.Id.ToString(),
                    IsSprinting = result
                });

            _world.BroadcastToAll(update);
        }
    }
}