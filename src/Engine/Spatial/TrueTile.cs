using System;

namespace DawnOfBlade.Engine.Spatial;

/// <summary>
/// An authoritative grid coordinate. The server owns the entity's exact True Tile; the client
/// interpolates a visual model toward it but never owns the value.
/// </summary>
public readonly record struct TrueTile(int X, int Y)
{
    public int ChebyshevDistance(TrueTile other) =>
        Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    public int ManhattanDistance(TrueTile other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>Absolute velocity profiles measured in tiles per 600 ms tick.</summary>
public enum MoveMode
{
    /// <summary>1 tile per tick.</summary>
    Walking,

    /// <summary>2 tiles per tick.</summary>
    Running,
}
