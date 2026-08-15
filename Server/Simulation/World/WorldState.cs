using SessionScape.Main.Protocol;
using System.Collections.Concurrent;

namespace SessionScape.Server.Simulation.World
{
    public class WorldState
    {
        public ConcurrentDictionary<Guid, ConnectedPlayer> Players { get; } = new();
        public ConcurrentDictionary<Guid, Entity> Entities { get; } = new();

        public void AddPlayer(ConnectedPlayer player)
        {
            Players[player.State.Id] = player;
            Entities[player.State.Id] = player.State;
        }

        public void RemovePlayer(Guid playerId)
        {
            Players.TryRemove(playerId, out _);
            Entities.TryRemove(playerId, out _);
        }

        public void BroadcastToAll(MessageEnvelope envelope)
        {
            foreach (var connectedPlayer in Players.Values)
            {
                TrySend(connectedPlayer, envelope);
            }
        }

        public void BroadcastToOthers(Guid excludeEntityId, MessageEnvelope envelope)
        {
            foreach (var connectedPlayer in Players.Values)
            {
                if (connectedPlayer.State.Id == excludeEntityId)
                    continue;

                TrySend(connectedPlayer, envelope);
            }
        }

        public void BroadcastToTargets(List<Guid> targetIds, MessageEnvelope envelope)
        {
            foreach (var targetId in targetIds)
            {
                if (!Players.TryGetValue(targetId, out var targetPlayer))
                    continue;

                TrySend(targetPlayer, envelope);
            }
        }

        // MessageFramer.WriteMessage is a synchronous, blocking
        // socket write with no timeout. TickLoop runs every handler and
        // every broadcast sequentially on one thread -- so if any single
        // client's socket stalls (full receive buffer, dead connection,
        // slow/buggy client), this write never returns, and it freezes the
        // ENTIRE server for EVERY player, forever, with no exception and no
        // log.
        private void TrySend(ConnectedPlayer connectedPlayer, MessageEnvelope envelope)
        {
            try
            {
                MessageFramer.WriteMessage(connectedPlayer.Stream, envelope);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldState] Send to player {connectedPlayer.State.Id} failed, dropping connection: {ex.Message}");
                RemovePlayer(connectedPlayer.State.Id);
            }
        }
    }
}