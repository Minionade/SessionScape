using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation.Handlers.Interaction
{
    public interface IInteractionHandler
    {
        InteractionTargetType TargetType { get; }
        InteractionVerb Verb { get; }
        void Handle(InteractionRequest data, PendingAction action, long currentTick);
    }

    public class InteractionResolver
    {
        private readonly Dictionary<(InteractionTargetType, InteractionVerb), IInteractionHandler> _handlers = new();
        public void Register(IInteractionHandler handler)
        {
            _handlers[(handler.TargetType, handler.Verb)] = handler; 
        }

        public bool TryResolve(InteractionRequest data, PendingAction action, long currentTick)
        {
            if (_handlers.TryGetValue((data.TargetType, data.Verb), out var handler))
            {
                handler.Handle(data, action, currentTick);
                return true;
            }

            return false;
        }
    }

    public class InteractionHandler : MessageHandler<InteractionRequest>
    {
        private readonly InteractionResolver _resolver;
        public InteractionHandler(InteractionResolver resolver) => _resolver = resolver;

        public override MessageType Type => MessageType.InteractionRequest;

        protected override void HandleTyped(InteractionRequest data, PendingAction action, long currentTick)
        {
            if (!_resolver.TryResolve(data, action, currentTick))
            {
                var response = MessageEnvelope.Create(MessageType.InteractionResponse, action.Envelope.Sequence, currentTick,
                    new InteractionResponse { Accepted = false, RejectReason = "No handler exists for interaction." });

                MessageFramer.WriteMessage(action.ResponseStream, response);
            }
        }
    }
}
