using System;
using System.Collections.Generic;

namespace SessionScape.Main.Protocol.Messages
{
    public class EntitySnapshot
    {
        public string Name { get; set; } = "Entity";
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int X { get; set; }
        public float Y { get; set; }
        public int Z { get; set; }
    }

    public class EntitySnapshotList
    {
        public List<EntitySnapshot> Entities { get; set; } = new();
    }

    public class EntityUpdate
    {
        public EntitySnapshot Entity { get; set; } = new();
    }

    public class EntityMovementUpdate
    {
        public string EntityId { get; set; } = string.Empty;
        public Waypoint[] Path { get; set; } = Array.Empty<Waypoint>();
    }

    public class EntityRemoved
    {
        public string EntityId { get; set; } = string.Empty;
    }
}