using SessionScape.Main.Protocol.Messages;

namespace SessionScape.Server.Simulation
{
    public static class EntitySnapshotHelper
    {
        public static EntitySnapshot AsSnapshot(this Entity entity)
        {
            return new EntitySnapshot
            {
                EntityId = entity.Id.ToString(),
                EntityType = entity.Type.ToString(),
                Name = entity.Name,
                X = entity.X,
                Y = entity.Y,
                Z = entity.Z,
            };
        }
    }
}
