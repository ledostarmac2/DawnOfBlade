using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Spatial;

/// <summary>
/// 4-connected breadth-first pathfinding that routes entities around tiles that block movement
/// (solids and low obstacles). Returns the shortest tile path excluding the start and including
/// the goal, or an empty path if unreachable. This is what forces melee AI around solid corners.
/// <para>
/// Every search accepts an optional <c>maxExpansion</c> budget (tiles dequeued before giving up).
/// Chasing AI re-paths every tick, so an unbounded flood-fill on a large open grid would be wasted
/// work; a budget keeps each re-path cheap and bounded. The default is unbounded so existing
/// callers and the static world keep their exact behavior.
/// </para>
/// </summary>
public static class GridPathfinder
{
    private static readonly (int Dx, int Dy)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    /// <summary>Shortest path from <paramref name="start"/> to <paramref name="goal"/> (goal included).</summary>
    public static IReadOnlyList<TrueTile> FindPath(
        CollisionGrid grid, TrueTile start, TrueTile goal, int maxExpansion = int.MaxValue)
    {
        if (start == goal || grid.BlocksMovement(start) || grid.BlocksMovement(goal))
        {
            return Array.Empty<TrueTile>();
        }

        return Search(grid, start, tile => tile == goal, maxExpansion);
    }

    /// <summary>
    /// Shortest path to any walkable tile orthogonally adjacent to <paramref name="target"/>.
    /// Used by chasers: the target tile itself is occupied by the quarry, so a melee pursuer must
    /// stop one tile away rather than route onto it. Returns an empty path when the chaser is
    /// already adjacent (it should attack, not move) or no adjacent tile is reachable.
    /// </summary>
    public static IReadOnlyList<TrueTile> FindPathAdjacent(
        CollisionGrid grid, TrueTile start, TrueTile target, int maxExpansion = int.MaxValue)
    {
        if (grid.BlocksMovement(start) || start.ManhattanDistance(target) <= 1)
        {
            return Array.Empty<TrueTile>();
        }

        return Search(grid, start, tile => tile.ManhattanDistance(target) == 1, maxExpansion);
    }

    private static IReadOnlyList<TrueTile> Search(
        CollisionGrid grid, TrueTile start, Func<TrueTile, bool> isGoal, int maxExpansion)
    {
        var cameFrom = new Dictionary<TrueTile, TrueTile>();
        var visited = new HashSet<TrueTile> { start };
        var queue = new Queue<TrueTile>();
        queue.Enqueue(start);
        var expanded = 0;

        while (queue.Count > 0)
        {
            if (expanded++ >= maxExpansion)
            {
                break;
            }

            var current = queue.Dequeue();

            foreach (var (dx, dy) in Directions)
            {
                var neighbour = new TrueTile(current.X + dx, current.Y + dy);
                if (visited.Contains(neighbour) || grid.BlocksMovement(neighbour))
                {
                    continue;
                }

                cameFrom[neighbour] = current;
                if (isGoal(neighbour))
                {
                    return Reconstruct(cameFrom, start, neighbour);
                }

                visited.Add(neighbour);
                queue.Enqueue(neighbour);
            }
        }

        return Array.Empty<TrueTile>();
    }

    private static IReadOnlyList<TrueTile> Reconstruct(
        IReadOnlyDictionary<TrueTile, TrueTile> cameFrom, TrueTile start, TrueTile goal)
    {
        var path = new List<TrueTile>();
        var current = goal;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}
