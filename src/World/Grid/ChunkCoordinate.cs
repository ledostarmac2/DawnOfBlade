namespace DawnOfBlade.World.Grid;

/// <summary>A network-interest chunk. Negative world coordinates use floor division.</summary>
public readonly record struct ChunkCoordinate(int X, int Z)
{
    public const int DefaultSize = 32;

    public static ChunkCoordinate FromTile(GridCoordinate tile, int chunkSize = DefaultSize)
    {
        if (chunkSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(chunkSize));
        }

        return new ChunkCoordinate(FloorDivide(tile.X, chunkSize), FloorDivide(tile.Z, chunkSize));
    }

    public bool IsWithinWindow(ChunkCoordinate center, int radius = 1)
    {
        if (radius < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(radius));
        }

        return System.Math.Abs(X - center.X) <= radius && System.Math.Abs(Z - center.Z) <= radius;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }
}
