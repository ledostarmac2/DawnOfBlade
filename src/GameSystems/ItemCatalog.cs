using System.Collections.Generic;

namespace DawnOfBlade.GameSystems;

/// <summary>Broad classification for a catalogued item (Part 17.2).</summary>
public enum ItemCategory
{
    Tool,
    Weapon,
    Armor,
    Consumable,
    Material,
}

/// <summary>
/// Engine-independent item definition (the data a Godot <c>ItemData</c> .tres would carry). Tools
/// expose a <see cref="ToolEfficiencyRating"/> that feeds gathering success checks.
/// </summary>
public sealed record ItemData(
    int ItemId,
    string Name,
    ItemCategory Category,
    bool Stackable,
    long Value,
    float ToolEfficiencyRating = 0f);

/// <summary>
/// A crafting/processing recipe (Part 17.2): raw inputs as [ItemId -> Quantity], a skill gate, and a
/// single output item id.
/// </summary>
public sealed record RecipeData(
    int OutputItemId,
    int OutputQuantity,
    int RequiredSkillLevel,
    IReadOnlyDictionary<int, int> Ingredients);
