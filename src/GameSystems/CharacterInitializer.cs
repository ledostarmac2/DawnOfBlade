using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems;

/// <summary>An item id + quantity to seed into a fresh character's first inventory slots.</summary>
public readonly record struct InventorySeed(int ItemId, int Quantity);

/// <summary>The full database insertion footprint produced for a brand-new character.</summary>
public sealed record FreshCharacter(
    PlayerRow Player,
    IReadOnlyList<InventoryRow> Inventory,
    IReadOnlyList<SkillRow> Skills);

/// <summary>
/// Builds the hardcoded fresh-spawn footprint (Part 20.1): a player row seated at the center of the
/// Verdant Valley safe zone, the starter tool kit in the first inventory slots, and every skill
/// written at 0.0 experience (Level 1). Pure data, so it can seed the local save now and a database
/// row insert later.
/// </summary>
public static class CharacterInitializer
{
    public static readonly GridCoordinate VerdantValleyCenter = new(0, 0);
    public const int StartingHealth = 10;   // Hitpoints level 1
    public const int StartingStamina = 100; // full run energy

    // Starter kit item ids (Part 20.1). Ids are local to the GameSystems schema; the content layer
    // maps them to display items.
    public const int BronzeHatchetItemId = 1001;
    public const int BronzePickaxeItemId = 1002;
    public const int BreadItemId = 2001;

    public static readonly IReadOnlyList<InventorySeed> DefaultStarterKit = new[]
    {
        new InventorySeed(BronzeHatchetItemId, 1),
        new InventorySeed(BronzePickaxeItemId, 1),
        new InventorySeed(BreadItemId, 10),
    };

    public static FreshCharacter Create(
        string characterId,
        string accountId,
        IEnumerable<string> skillIds,
        GridCoordinate? startCoordinate = null,
        IReadOnlyList<InventorySeed>? starterKit = null)
    {
        var coordinate = startCoordinate ?? VerdantValleyCenter;
        var player = new PlayerRow(characterId, accountId, coordinate, StartingHealth, StartingStamina, 0);

        var kit = starterKit ?? DefaultStarterKit;
        var inventory = new List<InventoryRow>();
        for (byte slot = 0; slot < kit.Count; slot++)
        {
            inventory.Add(new InventoryRow(characterId, slot, kit[slot].ItemId, kit[slot].Quantity));
        }

        var skills = skillIds
            .Select(skillId => new SkillRow(characterId, skillId, 0.0))
            .ToList();

        return new FreshCharacter(player, inventory, skills);
    }
}
