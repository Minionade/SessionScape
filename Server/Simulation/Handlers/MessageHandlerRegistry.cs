using SessionScape.Main.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation.Handlers
{
    public class MessageHandlerRegistry
    {
        private readonly Dictionary<MessageType, IMessageHandler> _handlers = new();

        public void Register(IMessageHandler handler)
        {
            _handlers[handler.Type] = handler;
        }

        public bool TryHandle(PendingAction action, long currentTick)
        {
            if (_handlers.TryGetValue(action.Envelope.Type, out var handler))
            {
                handler.Handle(action, currentTick);
                return true;
            }

            return false;
        }
    }
}
