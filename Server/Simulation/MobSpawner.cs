using SessionScape.Server.Simulation.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation
{
    public class SpawnRecord
    {
        public EntityType Type { get; set; }
        public (int x, int z) HomePosition { get; init; }
        public MobState CurrentMob { get; set; }
    }

    public class MobSpawner
    {
        private readonly WorldState _world;
        private readonly List<SpawnRecord> _spawnRecords = new();

        public MobSpawner(WorldState world)
        {
            _world = world;
        }

        public void RegisterSpawnPoint(EntityType type, int x, int z)
        {
            _spawnRecords.Add(new SpawnRecord { Type = type, HomePosition = (x, z) });
        }

        public void SpawnAll()
        {
            foreach (SpawnRecord record in _spawnRecords)
            {
                if (record.CurrentMob != null)
                    continue;

                MobState mob = new()
                {
                    Type = record.Type,
                    Name = record.Type.ToString(),
                    X = record.HomePosition.x,
                    Z = record.HomePosition.z,
                    HomePosition = record.HomePosition
                };

                record.CurrentMob = mob;
                _world.Entities[mob.Id] = mob;
            }
        }
    }
}
