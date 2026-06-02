using System.Collections.Generic;

namespace DawnOfBlade.World.Grid;

/// <summary>Rasterizes tile-aligned line of sight with Bresenham's algorithm.</summary>
public sealed class GridLineOfSight
{
    private readonly System.Func<GridCoordinate, bool> _blocksLineOfSight;

    public GridLineOfSight(System.Func<GridCoordinate, bool> blocksLineOfSight)
    {
        _blocksLineOfSight = blocksLineOfSight ?? throw new System.ArgumentNullException(nameof(blocksLineOfSight));
    }

    public bool HasClearPath(GridCoordinate source, GridCoordinate target)
    {
        foreach (var tile in Rasterize(source, target))
        {
            if (tile != source && tile != target && _blocksLineOfSight(tile))
            {
                return false;
            }
        }

        return true;
    }

    public static IEnumerable<GridCoordinate> Rasterize(GridCoordinate source, GridCoordinate target)
    {
        var x = source.X;
        var z = source.Z;
        var dx = System.Math.Abs(target.X - source.X);
        var dz = System.Math.Abs(target.Z - source.Z);
        var stepX = source.X < target.X ? 1 : -1;
        var stepZ = source.Z < target.Z ? 1 : -1;
        var error = dx - dz;

        while (true)
        {
            yield return new GridCoordinate(x, z);
            if (x == target.X && z == target.Z)
            {
                yield break;
            }

            var doubledError = 2 * error;
            if (doubledError > -dz)
            {
                error -= dz;
                x += stepX;
            }

            if (doubledError < dx)
            {
                error += dx;
                z += stepZ;
            }
        }
    }
}
