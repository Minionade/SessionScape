using SessionScape.Main.Protocol.Messages;
using SessionScape.Main.World;
using System.Collections;

namespace SessionScape.Server.Simulation.World
{
    public class Pathfinder
    {
        private readonly WorldMap _worldMap;
        private readonly PathRequestQueue _queue;

        public Pathfinder(WorldMap worldMap, PathRequestQueue queue)
        {
            _worldMap = worldMap;
            _queue = queue;
        }

        public void FindPath((int x, int z) startPosition, (int x, int z) endPosition)
        {
            Waypoint[] waypoints = [];
            bool pathSuccess = false;

            if (!_worldMap.TryGetTile(startPosition.x, startPosition.z, out Tile startTile))
            {
                _queue.FinishedProcessingPath(waypoints, false);
                return;
            }

            if (!_worldMap.TryGetTile(endPosition.x, endPosition.z, out Tile endTile))
            {
                _queue.FinishedProcessingPath(waypoints, false);
                return;
            }

            ResetTileForSearch(startTile);
            startTile.Parent = startTile;
            startTile.EnterDirection = (0, 0);

            List<Tile> neighbors = _worldMap.GetTileNeighbors(endTile, onlyValidTiles: true);
            if (!endTile.Walkable || neighbors.Count == 0)
            {
                endTile = _worldMap.GetClosestValidTile(endTile, startPosition.x, startPosition.z);
            }

            Heap<Tile> openSet = new(WorldConstants.ChunkSize * WorldConstants.ChunkSize);
            HashSet<Tile> closedSet = [];
            HashSet<Tile> touchedTiles = new() { startTile };

            openSet.Add(startTile);

            while (openSet.Count > 0)
            {
                Tile currentTile = openSet.RemoveFirst();

                closedSet.Add(currentTile);

                if (currentTile == endTile)
                {
                    pathSuccess = true;
                    break;
                }

                neighbors = _worldMap.GetTileNeighbors(currentTile, onlyValidTiles: true);

                foreach (Tile neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    if (touchedTiles.Add(neighbor))
                    {
                        ResetTileForSearch(neighbor);
                    }

                    int newCostToNeighbor = currentTile.GCost + GetDistance(currentTile, neighbor);

                    (int x, int z) newDirection = (neighbor.WorldX - currentTile.WorldX, neighbor.WorldZ - currentTile.WorldZ);

                    if (newDirection != currentTile.EnterDirection)
                    {
                        newCostToNeighbor += 1;
                    }

                    bool isInOpenSet = openSet.Contains(neighbor);

                    if (newCostToNeighbor < neighbor.GCost || !isInOpenSet)
                    {
                        neighbor.GCost = newCostToNeighbor;
                        neighbor.HCost = GetDistance(neighbor, endTile);
                        neighbor.Parent = currentTile;
                        neighbor.EnterDirection = newDirection;

                        if (!isInOpenSet)
                        {
                            openSet.Add(neighbor);
                        }
                        else
                        {
                            openSet.UpdateItem(neighbor);
                        }
                    }
                }
            }

            if (pathSuccess)
            {
                waypoints = RetracePath(startTile, endTile);
            }

            _queue.FinishedProcessingPath(waypoints, pathSuccess);
        }

        private void ResetTileForSearch(Tile tile)
        {
            tile.GCost = 0;
            tile.HCost = 0;
            tile.Parent = null;
            tile.EnterDirection = (0, 0);
            tile.HeapIndex = -1;
        }

        private int GetDistance(Tile tileA, Tile tileB)
        {
            int distanceX = Math.Abs(tileA.WorldX - tileB.WorldX);
            int distanceZ = Math.Abs(tileA.WorldZ - tileB.WorldZ);

            if (distanceX > distanceZ)
                return 14 * distanceZ + 10 * (distanceX - distanceZ);
            return 14 * distanceX + 10 * (distanceZ - distanceX);
        }

        private Waypoint[] CreateWaypoints(List<Tile> path)
        {
            List<Waypoint> waypoints = new();

            for (int i = 0; i < path.Count; i++)
            {
                waypoints.Add(new Waypoint
                {
                    X = path[i].WorldX,
                    Y = path[i].WorldY,
                    Z = path[i].WorldZ,
                });
            }

            return waypoints.ToArray();
        }

        private Waypoint[] RetracePath(Tile startTile, Tile endTile)
        {
            List<Tile> path = new();
            Tile currentTile = endTile;

            while (currentTile != startTile)
            {
                path.Add(currentTile);
                currentTile = currentTile.Parent;
            }

            Waypoint[] waypoints = CreateWaypoints(path);
            Array.Reverse(waypoints);
            return waypoints;
        }
    }
}