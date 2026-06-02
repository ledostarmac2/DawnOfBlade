namespace DawnOfBlade.World.Grid;

/// <summary>Validates one-tile movement while preventing traversal through sealed diagonal corners.</summary>
public sealed class GridMovementRules
{
    private readonly System.Func<GridCoordinate, bool> _isWalkable;

    public GridMovementRules(System.Func<GridCoordinate, bool> isWalkable)
    {
        _isWalkable = isWalkable ?? throw new System.ArgumentNullException(nameof(isWalkable));
    }

    public bool CanStep(GridCoordinate from, GridCoordinate to)
    {
        var deltaX = to.X - from.X;
        var deltaZ = to.Z - from.Z;
        if (System.Math.Abs(deltaX) > 1 || System.Math.Abs(deltaZ) > 1 || (deltaX == 0 && deltaZ == 0))
        {
            return false;
        }

        if (!_isWalkable(to))
        {
            return false;
        }

        if (deltaX == 0 || deltaZ == 0)
        {
            return true;
        }

        var flankX = new GridCoordinate(from.X + deltaX, from.Z);
        var flankZ = new GridCoordinate(from.X, from.Z + deltaZ);
        return _isWalkable(flankX) || _isWalkable(flankZ);
    }
}
