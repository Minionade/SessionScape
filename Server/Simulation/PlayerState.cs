using SessionScape.Server.Simulation.World;

namespace SessionScape.Server.Simulation
{
    public class PlayerState : Entity
    {
        public PlayerAccount Account { get; set; }

        public PlayerState()
        {
            Type = EntityType.Player;
        }
    }
}
