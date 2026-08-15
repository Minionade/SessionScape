using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;

namespace SessionScape.Server.Simulation.Handlers
{
    public class EchoHandler : MessageHandler<EchoRequest>
    {
        public override MessageType Type => MessageType.EchoRequest;

        protected override void HandleTyped(EchoRequest data, PendingAction action, long currentTick)
        {
            Console.WriteLine($"[Tick {currentTick}] Echo: {data.Text}");

            var response = MessageEnvelope.Create(MessageType.EchoResponse, action.Envelope.Sequence, currentTick,
                new EchoResponse { Text = data.Text });

            MessageFramer.WriteMessage(action.ResponseStream, response);
        }
    }
}
