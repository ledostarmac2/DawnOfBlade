using DawnOfBlade.World.Grid;

namespace DawnOfBlade.World.RiverValley;

public enum RegionAnchorType
{
    CharacterSpawn,
    Npc,
    Resource,
    ProcessingStation,
    Bridge,
    Staircase,
    Market,
}

/// <summary>Stable data-layer index entry for an interactive or navigational world feature.</summary>
public sealed record RegionAnchor(
    string Id,
    RegionAnchorType Type,
    GridCoordinate Coordinate,
    string InteractionType = "",
    int RequiredLevel = 1,
    bool BlocksMovement = false);
