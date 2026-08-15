using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System.Collections.Concurrent;

public class ChatResponseHandler : ClientMessageHandler<ChatResponse>
{
    private readonly ConcurrentQueue<string> _chatQueue;

    public override MessageType Type => MessageType.ChatResponse;

    public ChatResponseHandler(ConcurrentQueue<string> chatQueue) => _chatQueue = chatQueue;

    protected override void HandleTyped(ChatResponse data) 
    {
        // this one will change a lot later.  i want to add chat channels and PMs, announcements, etc.
        _chatQueue.Enqueue($"{data.PlayerName}: {data.Text}");
    }
}