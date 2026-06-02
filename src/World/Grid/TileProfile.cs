namespace DawnOfBlade.World.Grid;

public enum TileTriggerEffect
{
    None,
    PoisonDamage,
    SafeZone,
    Teleport,
}

/// <summary>Static tile metadata shared by pathfinding, line-of-sight, and zone triggers.</summary>
public sealed record TileProfile(
    int TileId,
    bool IsWalkable,
    bool IsLineOfSightBlocker,
    TileTriggerEffect TriggerEffect = TileTriggerEffect.None);
