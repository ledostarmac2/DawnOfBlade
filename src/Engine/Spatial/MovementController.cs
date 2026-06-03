using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Spatial;

/// <summary>The result of advancing movement during one tick.</summary>
/// <param name="Landing">The tile the entity ends the tick on (trigger-active).</param>
/// <param name="SkippedTiles">Intermediate tiles passed over while running (trigger-skipped).</param>
/// <param name="Moved">False if there was no path to advance.</param>
public readonly record struct TickMovement(TrueTile Landing, IReadOnlyList<TrueTile> SkippedTiles, bool Moved);

/// <summary>
/// Advances an entity along a queued path at the per-tick velocity defined by <see cref="Mode"/>.
/// Walking consumes 1 tile per tick; running consumes 2, passing through an intermediate tile and
/// landing on the second. Only the landing tile is trigger-active — intermediate tiles are
/// skipped entirely (no environmental triggers, obstacle bounds, or AoE traps).
/// </summary>
public sealed class MovementController
{
    private readonly Queue<TrueTile> _path = new();

    public MovementController(TrueTile start) => Position = start;

    public TrueTile Position { get; private set; }

    public MoveMode Mode { get; set; } = MoveMode.Walking;

    public bool HasPath => _path.Count > 0;

    public void SetPath(IEnumerable<TrueTile> tiles)
    {
        _path.Clear();
        foreach (var tile in tiles)
        {
            _path.Enqueue(tile);
        }
    }

    public void Stop() => _path.Clear();

    /// <summary>Advances one tick of movement and reports the landing and skipped tiles.</summary>
    public TickMovement Step()
    {
        if (_path.Count == 0)
        {
            return new TickMovement(Position, Array.Empty<TrueTile>(), Moved: false);
        }

        var steps = Mode == MoveMode.Running ? 2 : 1;
        var taken = new List<TrueTile>(steps);
        for (var i = 0; i < steps && _path.Count > 0; i++)
        {
            taken.Add(_path.Dequeue());
        }

        var landing = taken[^1];
        IReadOnlyList<TrueTile> skipped = taken.Count > 1
            ? taken.GetRange(0, taken.Count - 1)
            : Array.Empty<TrueTile>();

        Position = landing;
        return new TickMovement(landing, skipped, Moved: true);
    }
}
