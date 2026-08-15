using SessionScape.Main.Protocol;

public interface IClientMessageHandler
{
    MessageType Type { get; }
    void Handle(MessageEnvelope envelope);
}

public abstract class ClientMessageHandler<TData> : IClientMessageHandler
{
    public abstract MessageType Type { get; }
    public void Handle(MessageEnvelope envelope)
    {
        var data = envelope.GetData<TData>();
        HandleTyped(data);
    }

    protected abstract void HandleTyped(TData data);
}