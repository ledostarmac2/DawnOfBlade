using System.Collections.Generic;

namespace DawnOfBlade.World.Grid;

/// <summary>
/// Deterministic A* pathfinder for tile-aligned movement. Static obstacles block tiles;
/// dynamic actors intentionally do not so routes stay stable while entities move.
/// </summary>
public sealed class GridPathfinder
{
    private static readonly GridCoordinate[] NeighborOffsets =
    {
        new(0, -1),
        new(-1, 0),
        new(1, 0),
        new(0, 1),
    };

    private readonly System.Func<GridCoordinate, bool> _isWalkable;

    public GridPathfinder(System.Func<GridCoordinate, bool> isWalkable)
    {
        _isWalkable = isWalkable ?? throw new System.ArgumentNullException(nameof(isWalkable));
    }

    public IReadOnlyList<GridCoordinate> FindPath(GridCoordinate start, GridCoordinate destination)
    {
        if (start == destination)
        {
            return System.Array.Empty<GridCoordinate>();
        }

        if (!_isWalkable(destination))
        {
            return System.Array.Empty<GridCoordinate>();
        }

        var frontier = new PriorityQueue<GridCoordinate, (int Cost, int Sequence)>();
        var cameFrom = new Dictionary<GridCoordinate, GridCoordinate>();
        var costs = new Dictionary<GridCoordinate, int> { [start] = 0 };
        var sequence = 0;
        frontier.Enqueue(start, (0, sequence++));

        while (frontier.TryDequeue(out var current, out _))
        {
            if (current == destination)
            {
                return ReconstructPath(start, destination, cameFrom);
            }

            foreach (var offset in NeighborOffsets)
            {
                var next = new GridCoordinate(current.X + offset.X, current.Z + offset.Z);
                if (!_isWalkable(next))
                {
                    continue;
                }

                var nextCost = costs[current] + 1;
                if (costs.TryGetValue(next, out var knownCost) && nextCost >= knownCost)
                {
                    continue;
                }

                costs[next] = nextCost;
                cameFrom[next] = current;
                var priority = nextCost + ManhattanDistance(next, destination);
                frontier.Enqueue(next, (priority, sequence++));
            }
        }

        return System.Array.Empty<GridCoordinate>();
    }

    private static int ManhattanDistance(GridCoordinate a, GridCoordinate b) =>
        System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Z - b.Z);

    private static IReadOnlyList<GridCoordinate> ReconstructPath(
        GridCoordinate start,
        GridCoordinate destination,
        IReadOnlyDictionary<GridCoordinate, GridCoordinate> cameFrom)
    {
        var path = new List<GridCoordinate>();
        var current = destination;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}
