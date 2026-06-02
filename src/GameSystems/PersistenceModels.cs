using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems;

/// <summary>
/// Flat persistence row models mirroring the relational tables in Part 17.1. They are pure data so
/// the same shapes serialize to the local save today and to SQL rows when the backend lands
/// (see docs/PRODUCTION_BACKEND_ARCHITECTURE.md). Gold is a 64-bit integer to prevent economy
/// overflow; coordinates round-trip through the "X,Z" text form used by the Players table.
/// </summary>
public sealed record PlayerRow(
    string CharacterId,
    string AccountId,
    GridCoordinate Coordinates,
    int CurrentHealth,
    int CurrentStamina,
    long WalletGold)
{
    public string CoordinatesText => $"{Coordinates.X},{Coordinates.Z}";

    public static GridCoordinate ParseCoordinates(string text)
    {
        var parts = text.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var x) &&
            int.TryParse(parts[1], out var z))
        {
            return new GridCoordinate(x, z);
        }

        return new GridCoordinate(0, 0);
    }
}

/// <summary>One skill's experience for a character (Skills table). Skill ids stay string-keyed to
/// match the rest of the codebase; experience is double precision per the schema.</summary>
public sealed record SkillRow(string CharacterId, string SkillId, double ExperiencePoints);

/// <summary>One inventory slot for a character (Inventories table). SlotIndex is 0..27.</summary>
public sealed record InventoryRow(string CharacterId, byte SlotIndex, int ItemId, int Quantity)
{
    public const byte MaxSlotIndex = 27;
}
