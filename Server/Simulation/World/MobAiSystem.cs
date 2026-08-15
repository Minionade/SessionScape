using Server.Simulation.World;
using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System;

namespace SessionScape.Server.Simulation.World
{
    public class MobAiSystem
    {
        private readonly WorldState _world;
        private readonly PathRequestQueue _pathRequestQueue;
        private readonly Random _random = new();

        public MobAiSystem(WorldState world, PathRequestQueue pathRequestQueue)
        {
            _world = world;
            _pathRequestQueue = pathRequestQueue;
            TickLoop.OnTick += Tick;
        }

        private void Tick(long currentTick)
        {
            foreach (Entity entity in _world.Entities.Values)
            {
                if (entity is not MobState mob)
                    continue;

                switch (mob.AiState)
                {
                    case MobAiState.Idle:
                        if (currentTick >= mob.NextDecisionTick)
                            DecideWander(mob, currentTick);
                        break;

                    case MobAiState.Wandering:
                        if (!mob.Movement.IsMoving)
                            ReturnToIdle(mob, currentTick);
                        break;
                }
            }
        }

        private void DecideWander(MobState mob, long currentTick)
        {
            MobDefinition def = MobDefinitions.Get(mob.Type);

            int offsetX = _random.Next(-def.WanderRadius, def.WanderRadius + 1);
            int offsetZ = _random.Next(-def.WanderRadius, def.WanderRadius + 1);

            (int x, int z) target = (mob.HomePosition.x + offsetX, mob.HomePosition.z + offsetZ);

            mob.AiState = MobAiState.Wandering;

            _pathRequestQueue.RequestPath((mob.X, mob.Z), target, (path, success) =>
            {
                mob.Movement.SetPath(path, success);

                if (!success)
                {
                    ReturnToIdle(mob, currentTick);
                    return;
                }

                var movementUpdate = MessageEnvelope.Create(
                    MessageType.EntityMovementUpdate,
                    0,
                    currentTick,
                    new EntityMovementUpdate
                    {
                        EntityId = mob.Id.ToString(),
                        Path = path
                    });

                _world.BroadcastToAll(movementUpdate);
            });
        }

        private void ReturnToIdle(MobState mob, long currentTick)
        {
            MobDefinition def = MobDefinitions.Get(mob.Type);

            mob.AiState = MobAiState.Idle;
            mob.NextDecisionTick = currentTick + _random.Next(def.MinIdleTicks, def.MaxIdleTicks + 1);
        }
    }
}