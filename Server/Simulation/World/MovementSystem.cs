using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;

namespace SessionScape.Server.Simulation.World
{
    public class MovementSystem
    {
        private readonly WorldState _world;
        private readonly HashSet<string> _subscribedEntities = new();

        public MovementSystem(WorldState world)
        {
            _world = world;

            foreach (Entity entity in _world.Entities.Values)
            {
                SubscribeToMovement(entity);
            }

            TickLoop.OnTick += Tick;
        }

        private void SubscribeToMovement(Entity entity)
        {
            if (!_subscribedEntities.Add(entity.Id.ToString()))
                return;

            entity.Movement.OnPathSet += (path) =>
            {
                var update = MessageEnvelope.Create(MessageType.EntityMovementUpdate, 0, 0,
                    new EntityMovementUpdate
                    {
                        EntityId = entity.Id.ToString(),
                        Path = path
                    });

                _world.BroadcastToAll(update);
            };

            entity.Movement.OnRunCancelled += () =>
            {
                entity.Movement.TrySetRun(false);

                var update = MessageEnvelope.Create(MessageType.RunUpdate, 0, 0,
                    new RunUpdate
                    {
                        EntityId = entity.Id.ToString(),
                        IsSprinting = false
                    });

                _world.BroadcastToAll(update);
            };

            entity.Movement.OnRunEnergyUpdated += (amount) =>
            {
                var update = MessageEnvelope.Create(MessageType.RunEnergyUpdate, 0, 0,
                    new RunEnergyUpdate
                    {
                        RunEnergy = amount
                    });

                _world.BroadcastToTargets([entity.Id], update);
            };
        }

        private void Tick(long currentTick)
        {
            foreach (Entity entity in _world.Entities.Values)
            {
                SubscribeToMovement(entity);

                if (!entity.Movement.Tick(currentTick))
                    continue;

                var update = MessageEnvelope.Create(MessageType.EntityUpdate, 0, currentTick,
                    new EntityUpdate
                    {
                        Entity = EntitySnapshotHelper.AsSnapshot(entity)
                    });

                _world.BroadcastToAll(update);
            }
        }
    }
}