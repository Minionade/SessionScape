using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System.Diagnostics;

public class InteractionResponseHandler : ClientMessageHandler<InteractionResponse>
{
    public override MessageType Type => MessageType.InteractionResponse;
    protected override void HandleTyped(InteractionResponse data)
    {
        if (!data.Accepted)
            UnityEngine.Debug.Log("Interaction Rejected: " +  data.RejectReason);
    }
}