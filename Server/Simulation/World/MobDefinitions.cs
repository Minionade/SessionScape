using SessionScape.Server.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Simulation.World
{
    public class MobDefinition
    {
        public int WanderRadius { get; set; }
        public int MinIdleTicks { get; set; }
        public int MaxIdleTicks { get; set; }

    }

    public static class MobDefinitions
    {
        public static readonly Dictionary<EntityType, MobDefinition> ByType = new()
        {
            [EntityType.Npc] = new MobDefinition
            {
                WanderRadius = 5,
                MinIdleTicks = 3,
                MaxIdleTicks = 10
            }
        };

        public static MobDefinition Get(EntityType type)
        {
            return ByType.TryGetValue(type, out MobDefinition def) ? def : ByType[EntityType.Npc];
        }
    }
}
