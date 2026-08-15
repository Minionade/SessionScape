using SessionScape.Main.Protocol;
using System.Collections.Generic;

public class ClientMessageHandlerRegistry
{
    private readonly Dictionary<MessageType, IClientMessageHandler> _handlers = new();

    public void Register(IClientMessageHandler handler)
    {
        _handlers[handler.Type] = handler;
    }

    public bool TryHandle(MessageEnvelope envelope)
    {
        if (_handlers.TryGetValue(envelope.Type, out var handler))
        {
            handler.Handle(envelope);
            return true;
        }

        return false;
    }
}