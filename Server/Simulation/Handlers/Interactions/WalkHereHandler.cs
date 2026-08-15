using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Server.Simulation.Handlers.Interaction;
using SessionScape.Server.Simulation.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation.Handlers.Interactions
{
    public class WalkHereHandler : IInteractionHandler
    {
        private readonly WorldState _world;
        private readonly ChunkStreamer _chunkStreamer;
        private readonly PathRequestQueue _pathRequestQueue;

        public WalkHereHandler(WorldState world, ChunkStreamer chunkStreamer, PathRequestQueue pathRequestQueue)
        {
            _world = world;
            _chunkStreamer = chunkStreamer;
            _pathRequestQueue = pathRequestQueue;
        }

        public InteractionTargetType TargetType => InteractionTargetType.Tile;
        public InteractionVerb Verb => InteractionVerb.WalkHere;


        public void Handle(InteractionRequest data, PendingAction action, long currentTick)
        {
            var startPosition = (action.Player.X, action.Player.Z);
            var endPosition = (data.TargetX.Value, data.TargetZ.Value);

            _pathRequestQueue.RequestPath(startPosition, endPosition, (path, success) =>
            {
                action.Player.Movement.SetPath(path, success);

                var response = MessageEnvelope.Create(MessageType.InteractionResponse,
                    action.Envelope.Sequence, currentTick,
                    new InteractionResponse { Accepted =  success, RejectReason = success ? "" : "Unable to find path." });

                MessageFramer.WriteMessage(action.ResponseStream, response);

                if (!success)
                    return;

                var movementUpdate = MessageEnvelope.Create(MessageType.EntityMovementUpdate,
                     action.Envelope.Sequence, currentTick,
                    new EntityMovementUpdate { EntityId = action.Player.Id.ToString(), Path = path });

                _world.BroadcastToAll(movementUpdate);
            });
        }
    }
}
