using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System.Diagnostics;

public class EchoResponseHandler : ClientMessageHandler<EchoResponse>
{
    public override MessageType Type => MessageType.EchoResponse;

    protected override void HandleTyped(EchoResponse data)
    {
        UnityEngine.Debug.Log("[Server Echo] " + data.Text);
    }
}