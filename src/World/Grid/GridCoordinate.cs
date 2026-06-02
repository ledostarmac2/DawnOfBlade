namespace DawnOfBlade.World.Grid;

/// <summary>A discrete server-authoritative tile coordinate mapped to the X/Z world plane.</summary>
public readonly record struct GridCoordinate(int X, int Z)
{
    public int ChebyshevDistanceTo(GridCoordinate other) =>
        System.Math.Max(System.Math.Abs(X - other.X), System.Math.Abs(Z - other.Z));

    public ChunkCoordinate ToChunk(int chunkSize = ChunkCoordinate.DefaultSize) =>
        ChunkCoordinate.FromTile(this, chunkSize);
}
