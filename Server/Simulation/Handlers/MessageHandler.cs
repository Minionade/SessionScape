using SessionScape.Main.Protocol;

namespace SessionScape.Server.Simulation.Handlers
{
    public interface IMessageHandler
    {
        MessageType Type { get; }
        void Handle(PendingAction action, long currentTick);
    }

    public abstract class MessageHandler<TRequest> : IMessageHandler
    {
        public abstract MessageType Type { get; }

        public void Handle(PendingAction action, long currentTick)
        {
            var data = action.Envelope.GetData<TRequest>();
            HandleTyped(data, action, currentTick);
        }

        protected abstract void HandleTyped(TRequest data, PendingAction action, long currentTick);
    }
}
