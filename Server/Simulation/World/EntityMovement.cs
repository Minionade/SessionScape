using SessionScape.Main.Protocol.Messages;
using System;

namespace SessionScape.Server.Simulation.World
{
    public class EntityMovement
    {

        protected readonly Entity _entity;

        protected Waypoint[] _path = Array.Empty<Waypoint>();
        protected int _pathIndex;

        public event Action<Waypoint[]> OnPathSet;
        public event Action<double> OnRunEnergyUpdated;
        public event Action OnRunCancelled;

        public bool IsMoving => _pathIndex < _path.Length;
        public double RunEnergy { get; private set; } = 100;
        public bool IsRunning { get; private set; }

        public Entity Entity => _entity;

        public EntityMovement(Entity entity)
        {
            _entity = entity;
        }

        public bool TrySetRun(bool isRunning)
        {
            if (isRunning && RunEnergy <= 1)
                return false;

            IsRunning = isRunning;
            return true;
        }

        public virtual void SetPath(Waypoint[] path, bool success)
        {
            if (path == null || path.Length == 0 || !success)
            {
                Stop();
                return;
            }

            _path = path;
            _pathIndex = 0;

            OnPathSet?.Invoke(_path);
        }

        public void Stop()
        {
            _path = Array.Empty<Waypoint>();
            _pathIndex = 0;
        }

        public virtual bool Tick(long currentTick)
        {
            if (!IsMoving)
                return false;

            Waypoint waypoint = _path[_pathIndex];

            _entity.X = waypoint.X;
            _entity.Y = waypoint.Y;
            _entity.Z = waypoint.Z;

            if (IsRunning && CanRun())
            {
                _pathIndex += 2;
                ConsumeRunEnergy();
            }
            else
            {
                _pathIndex++;
            }

            if (_pathIndex >= _path.Length)
            {
                Stop();
            }

            return true;
        }

        private bool CanRun()
        {
            if (RunEnergy < 1)
            {
                IsRunning = false;
                OnRunCancelled?.Invoke();
                return false;
            }

            if (_pathIndex + 2 >= _path.Length)
                return false;

            Waypoint currentWaypoint = _path[_pathIndex];
            Waypoint nextWaypoint = _path[_pathIndex + 1];
            Waypoint followingWaypoint = _path[_pathIndex + 2];

            int currentDirectionX = nextWaypoint.X - currentWaypoint.X;
            int currentDirectionZ = nextWaypoint.Z - currentWaypoint.Z;

            int nextDirectionX = followingWaypoint.X - nextWaypoint.X;
            int nextDirectionZ = followingWaypoint.Z - nextWaypoint.Z;

            return currentDirectionX == nextDirectionX &&
                   currentDirectionZ == nextDirectionZ;
        }

        private void ConsumeRunEnergy()
        {
            int agility = 1; // add skills!
            double unitsLost = 60 * (1 - (agility / 300)) * 0.01;
            Console.WriteLine("[EntityMovement] Run Energy Lost: " + unitsLost);

            if (unitsLost <= 0)
                return;

            RunEnergy -= unitsLost;

            if (RunEnergy < 0)
                RunEnergy = 0;

            OnRunEnergyUpdated?.Invoke(RunEnergy);
        }

        public void RestoreRunEnergy(long currentTick)
        {
            if (RunEnergy == 100 || (IsMoving && IsRunning))
                return;

            int agility = 1; // add skills!

            double restoreAmount = ((agility / 10) + 15) * agility * 0.01;
            Console.WriteLine("[EntityMovement] Run Energy Restored: " + restoreAmount);

            if (restoreAmount <= 0)
                return;

            RunEnergy += restoreAmount;

            if (RunEnergy > 100)
                RunEnergy = 100;

            OnRunEnergyUpdated?.Invoke(RunEnergy);
        }

        public void SetRunEnergy(double energy)
        {
            RunEnergy = energy;
            OnRunEnergyUpdated?.Invoke(RunEnergy);
        }
    }
}