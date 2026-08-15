using SessionScape.Server.Simulation.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionScape.Server.Simulation
{
    public enum EntityType
    {
        Player,
        Npc,
    }

    public class Entity
    {
        public Guid Id { get; } = Guid.NewGuid();
        public EntityType Type { get; set; } = EntityType.Player;
        public string Name { get; set; } = "Entity";
        public int X { get; set; }
        public float Y { get; set; }
        public int Z { get; set; }

        public EntityMovement Movement { get; }

        public Entity()
        {
            Movement = new EntityMovement(this);
        }
    }
}
