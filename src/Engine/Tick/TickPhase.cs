namespace DawnOfBlade.Engine.Tick;

/// <summary>
/// Execution order for actions resolved within a single 600 ms tick. Lower phases run first,
/// so an interface toggle (Phase 0) is always applied before combat (Phase 3) reads it.
/// </summary>
public enum TickPhase
{
    /// <summary>UI interactions, inventory reordering, equipment/overhead swaps.</summary>
    Interface = 0,

    /// <summary>Item usage, resource consumption, and health modifications.</summary>
    Consumption = 1,

    /// <summary>Coordinate translation and movement pathfinding.</summary>
    Movement = 2,

    /// <summary>Accuracy rolls, damage distribution, and mitigation evaluation.</summary>
    Combat = 3,
}
