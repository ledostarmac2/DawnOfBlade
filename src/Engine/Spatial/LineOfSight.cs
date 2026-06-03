using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Spatial;

/// <summary>
/// Straight-line raycasting for projectiles and spells. A projectile path is blocked only by
/// <c>solid</c> tiles; it passes freely over <c>low obstacle</c> tiles. Combined with movement
/// pathfinding (which routes around solids), this lets a ranged attacker hit a target over a
/// low wall that the target's melee AI cannot reach.
/// </summary>
public static class LineOfSight
{
    /// <summary>True if a projectile can travel from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static bool HasProjectilePath(CollisionGrid grid, TrueTile from, TrueTile to)
    {
        foreach (var tile in InteriorTiles(from, to))
        {
            if (grid.IsSolid(tile))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Bresenham tiles strictly between the endpoints (endpoints excluded).</summary>
    public static IEnumerable<TrueTile> InteriorTiles(TrueTile from, TrueTile to)
    {
        int x = from.X, y = from.Y;
        var dx = Math.Abs(to.X - from.X);
        var dy = -Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            var atEndpoint = (x == from.X && y == from.Y) || (x == to.X && y == to.Y);
            if (!atEndpoint)
            {
                yield return new TrueTile(x, y);
            }

            if (x == to.X && y == to.Y)
            {
                yield break;
            }

            var e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x += sx;
            }

            if (e2 <= dx)
            {
                error += dx;
                y += sy;
            }
        }
    }
}
