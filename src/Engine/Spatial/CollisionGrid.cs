namespace DawnOfBlade.Engine.Spatial;

/// <summary>
/// A flat 2D collision plane with two layers: <c>solid</c> tiles block both movement and
/// line of sight; <c>low obstacle</c> tiles block movement but can be shot/cast over. The
/// difference between the two layers is what enables line-of-sight trapping ("safespotting").
/// Out-of-bounds tiles are treated as solid.
/// </summary>
public sealed class CollisionGrid
{
    private readonly bool[,] _solid;
    private readonly bool[,] _lowObstacle;

    public CollisionGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _solid = new bool[width, height];
        _lowObstacle = new bool[width, height];
    }

    public int Width { get; }
    public int Height { get; }

    public bool InBounds(TrueTile tile) =>
        tile.X >= 0 && tile.X < Width && tile.Y >= 0 && tile.Y < Height;

    public bool IsSolid(TrueTile tile) => !InBounds(tile) || _solid[tile.X, tile.Y];

    public bool IsLowObstacle(TrueTile tile) => InBounds(tile) && _lowObstacle[tile.X, tile.Y];

    /// <summary>Blocks movement if the tile is solid or a low obstacle.</summary>
    public bool BlocksMovement(TrueTile tile) => IsSolid(tile) || IsLowObstacle(tile);

    public void SetSolid(int x, int y, bool value = true) => _solid[x, y] = value;

    public void SetLowObstacle(int x, int y, bool value = true) => _lowObstacle[x, y] = value;
}
