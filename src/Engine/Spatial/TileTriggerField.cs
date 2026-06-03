using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Spatial;

/// <summary>
/// Tracks trap/trigger tiles and evaluates which fire for a given tick of movement. Triggers
/// fire only on the landing tile; tiles skipped while running are bypassed entirely.
/// </summary>
public sealed class TileTriggerField
{
    private readonly HashSet<TrueTile> _traps = new();

    public void AddTrap(TrueTile tile) => _traps.Add(tile);

    public bool HasTrap(TrueTile tile) => _traps.Contains(tile);

    /// <summary>Returns the traps sprung by this movement (landing tile only).</summary>
    public IReadOnlyList<TrueTile> Evaluate(TickMovement movement)
    {
        if (movement.Moved && _traps.Contains(movement.Landing))
        {
            return new[] { movement.Landing };
        }

        return Array.Empty<TrueTile>();
    }
}
