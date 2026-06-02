namespace DawnOfBlade.World.RiverValley;

/// <summary>Server-facing population budget for a bounded ecosystem area.</summary>
public sealed record SpawnPoolDefinition(
    string Id,
    string EntityId,
    GridBounds Bounds,
    int MaximumActive,
    int RespawnTicks,
    bool IsAggressive,
    int AggroRadiusTiles = 0);
