using System.Collections.Generic;
using DawnOfBlade.World.Grid;

namespace DawnOfBlade.World.RiverValley;

/// <summary>Inclusive tile-aligned rectangle used by region rules and spawn pools.</summary>
public readonly record struct GridBounds(GridCoordinate Minimum, GridCoordinate Maximum)
{
    public bool Contains(GridCoordinate coordinate) =>
        coordinate.X >= Minimum.X && coordinate.X <= Maximum.X &&
        coordinate.Z >= Minimum.Z && coordinate.Z <= Maximum.Z;

    public IEnumerable<GridCoordinate> Tiles()
    {
        for (var z = Minimum.Z; z <= Maximum.Z; z++)
        {
            for (var x = Minimum.X; x <= Maximum.X; x++)
            {
                yield return new GridCoordinate(x, z);
            }
        }
    }
}
