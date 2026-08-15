using SessionScape.Main.Protocol.Messages;

namespace SessionScape.Server.Simulation.World
{
    public class PathRequestQueue
    {
        struct PathRequest
        {
            public (int x, int z) PathStart;
            public (int x, int z) PathEnd;
            public Action<Waypoint[], bool> Callback;
        }

        private Pathfinder _pathfinder;

        private Queue<PathRequest> requestQueue = new();
        private PathRequest currentRequest;

        private bool isRunning;

        public PathRequestQueue(WorldMap parent)
        {
            _pathfinder = new(parent, this);
        }

        private void TryProcessNext()
        {
            if (!isRunning && requestQueue.Count > 0)
            {
                currentRequest = requestQueue.Dequeue();
                isRunning = true;

                _pathfinder.FindPath(currentRequest.PathStart, currentRequest.PathEnd);
            }
        }

        public void FinishedProcessingPath(Waypoint[] path, bool success)
        {
            currentRequest.Callback(path, success);

            isRunning = false;

            TryProcessNext();
        }

        public void RequestPath((int x, int z) pathStart, (int x, int z) pathEnd, Action<Waypoint[], bool> callback)
        {
            PathRequest newRequest = new PathRequest
            {
                PathStart = pathStart,
                PathEnd = pathEnd,
                Callback = callback
            };

            requestQueue.Enqueue(newRequest);
            TryProcessNext();
        }

    }
}
