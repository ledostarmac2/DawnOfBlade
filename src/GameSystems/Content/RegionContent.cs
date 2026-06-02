using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems.Content;

/// <summary>
/// Item ids used by the starting-region content. These are the GameSystems integer ids (Part 17);
/// the Godot content/visual layer maps them to display items independently.
/// </summary>
public static class RegionItemIds
{
    public const int Coins = 1000;
    public const int CopperOre = 3001;
    public const int TinOre = 3002;
    public const int SoftwoodLogs = 3003;
    public const int RawWool = 3004;
    public const int BronzeBar = 3100;
    public const int BronzeDagger = 3101;
    public const int Feathers = 4001;
    public const int RawPoultry = 4002;
    public const int BrittleBones = 4003;
}

/// <summary>How a player engages a fixed interaction node from the Part 25 gameplay-loop map.</summary>
public enum InteractionType
{
    Mining,
    Woodcutting,
    Cooking,
    Crafting,
}

/// <summary>
/// A hardcoded starting-loop interaction point (Part 25): a grid tile, the skill used, the level
/// gate, the item it yields (0 for a processing station that consumes rather than yields), and how
/// many ticks it stays depleted before <see cref="ResourceSpawnerPool"/> regrows it.
/// </summary>
public sealed record RegionInteractionNode(
    string NodeId,
    GridCoordinate Coordinate,
    InteractionType Interaction,
    int RequiredLevel,
    int ItemId,
    int RespawnTicks)
{
    public bool IsProcessingStation => ItemId == 0;
}

/// <summary>
/// Engine-independent, server-authoritative content for the River Valley starting region
/// (Parts 22-25). It carries only gameplay data — coordinates, skills, levels, yields, the bronze
/// smelting recipe — and references nothing in Godot, so it complements (does not duplicate) the
/// scene/world generation. The Part 25 table is reproduced verbatim as the canonical loop map.
/// </summary>
public static class StartingRegion
{
    /// <summary>Side length of the 128x128 tile sub-grid (Part 22).</summary>
    public const int RegionSize = 128;

    /// <summary>Courtyard respawn zero-point — every defeat returns the player here (Part 22.1).</summary>
    public static readonly GridCoordinate RespawnPoint = new(35, 35);

    /// <summary>The toll bridge tiles gating the wild Eastern Sector (Part 23.2).</summary>
    public static readonly GridCoordinate BridgeWest = new(51, 35);
    public static readonly GridCoordinate BridgeEast = new(60, 35);

    private const int OreRespawnTicks = 50;
    private const int TreeRespawnTicks = 30;

    /// <summary>Smelting loop: 1 Copper + 1 Tin -> 1 Bronze Bar (Parts 5 / 23.2).</summary>
    public static readonly RecipeData BronzeRecipe = new(
        OutputItemId: RegionItemIds.BronzeBar,
        OutputQuantity: 1,
        RequiredSkillLevel: 1,
        Ingredients: new Dictionary<int, int>
        {
            [RegionItemIds.CopperOre] = 1,
            [RegionItemIds.TinOre] = 1,
        });

    /// <summary>
    /// Canonical gameplay metadata for the 26 live harvestable River Valley nodes and its two
    /// processing stations. Node ids and coordinates intentionally match the live world index.
    /// </summary>
    public static readonly IReadOnlyList<RegionInteractionNode> InteractionNodes = new[]
    {
        new RegionInteractionNode("copper_ore_01", new GridCoordinate(85, 15), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("copper_ore_02", new GridCoordinate(87, 14), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("copper_ore_03", new GridCoordinate(89, 17), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("copper_ore_04", new GridCoordinate(91, 13), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("copper_ore_05", new GridCoordinate(93, 18), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("copper_ore_06", new GridCoordinate(84, 20), InteractionType.Mining, 1, RegionItemIds.CopperOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_01", new GridCoordinate(95, 16), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_02", new GridCoordinate(97, 15), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_03", new GridCoordinate(99, 18), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_04", new GridCoordinate(101, 14), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_05", new GridCoordinate(103, 20), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("tin_ore_06", new GridCoordinate(105, 17), InteractionType.Mining, 1, RegionItemIds.TinOre, OreRespawnTicks),
        new RegionInteractionNode("softwood_tree_01", new GridCoordinate(70, 60), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_02", new GridCoordinate(74, 63), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_03", new GridCoordinate(78, 58), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_04", new GridCoordinate(82, 66), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_05", new GridCoordinate(87, 62), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_06", new GridCoordinate(91, 70), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_07", new GridCoordinate(96, 55), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_08", new GridCoordinate(100, 74), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_09", new GridCoordinate(105, 64), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_10", new GridCoordinate(109, 79), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_11", new GridCoordinate(114, 58), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_12", new GridCoordinate(116, 85), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_13", new GridCoordinate(72, 82), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("softwood_tree_14", new GridCoordinate(88, 87), InteractionType.Woodcutting, 1, RegionItemIds.SoftwoodLogs, TreeRespawnTicks),
        new RegionInteractionNode("castle_cooking_hearth", new GridCoordinate(45, 35), InteractionType.Cooking, 1, 0, 0),
        new RegionInteractionNode("castle_spinning_wheel", new GridCoordinate(25, 40), InteractionType.Crafting, 1, 0, 0),
    };

    public static readonly IReadOnlyDictionary<string, RegionInteractionNode> InteractionNodesById =
        InteractionNodes.ToDictionary(node => node.NodeId, System.StringComparer.Ordinal);

    public static IEnumerable<RegionInteractionNode> NodesOfType(InteractionType type) =>
        InteractionNodes.Where(node => node.Interaction == type);

    public static bool TryGetNode(string nodeId, out RegionInteractionNode node) =>
        InteractionNodesById.TryGetValue(nodeId, out node!);

    public static RegionInteractionNode GetNode(string nodeId) =>
        TryGetNode(nodeId, out var node)
            ? node
            : throw new KeyNotFoundException($"No starting-region interaction node is registered for '{nodeId}'.");

    /// <summary>A spawner pool pre-seeded with every harvestable (non-processing) node tile.</summary>
    public static ResourceSpawnerPool CreateResourceSpawnerPool() =>
        new(InteractionNodes
            .Where(node => !node.IsProcessingStation)
            .Select(node => new ResourceSpawnAnchor(node.NodeId, node.Coordinate)));

    /// <summary>True for tiles west of the river channel — the hard safe zone (Part 23.1).</summary>
    public static bool IsWesternSafeSector(GridCoordinate tile) => tile.X < BridgeWest.X;
}
