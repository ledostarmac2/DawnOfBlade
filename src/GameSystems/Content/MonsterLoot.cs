using System.Collections.Generic;

namespace DawnOfBlade.GameSystems.Content;

/// <summary>The combat-triangle role a creature fights with (Part 4 / Part 23).</summary>
public enum CombatStyle
{
    Melee,
    Marksman,
    Arcane,
}

/// <summary>
/// Engine-independent definition of a starting-region creature: its level, hitpoints, combat style,
/// aggression, and the <see cref="LootTable"/> rolled on death by <see cref="LootRoller"/>.
/// </summary>
public sealed record MonsterArchetype(
    string Id,
    string Name,
    int Level,
    int MaxHitpoints,
    CombatStyle Style,
    bool Aggressive,
    int AggroRadius,
    LootTable Loot);

/// <summary>
/// The four baseline River Valley creatures and their drop tables (Parts 23.1-23.2). Drops use the
/// GameSystems <see cref="LootTable"/> tiers so they roll deterministically through the same engine
/// as every other drop.
/// </summary>
public static class StartingRegionMonsters
{
    /// <summary>Domestic poultry: zero defence, guaranteed feathers + raw poultry (Part 23.1).</summary>
    public static readonly MonsterArchetype Chicken = new(
        "chicken", "Domestic Poultry", Level: 1, MaxHitpoints: 3, CombatStyle.Marksman,
        Aggressive: false, AggroRadius: 0,
        new LootTable(guaranteed: new[]
        {
            new LootDrop(RegionItemIds.Feathers, 1),
            new LootDrop(RegionItemIds.RawPoultry, 1),
        }));

    /// <summary>Graveyard skeleton: passive-hostile melee, guaranteed brittle bones (Part 23.1).</summary>
    public static readonly MonsterArchetype Skeleton = new(
        "reanimated_skeleton", "Reanimated Skeleton", Level: 2, MaxHitpoints: 8, CombatStyle.Melee,
        Aggressive: false, AggroRadius: 0,
        new LootTable(guaranteed: new[] { new LootDrop(RegionItemIds.BrittleBones, 1) }));

    /// <summary>Forest marauder: aggressive melee, guaranteed coins + a chance at bronze (Part 23.2).</summary>
    public static readonly MonsterArchetype Goblin = new(
        "forest_marauder", "Forest Marauder", Level: 3, MaxHitpoints: 11, CombatStyle.Melee,
        Aggressive: true, AggroRadius: 3,
        new LootTable(
            guaranteed: new[] { new LootDrop(RegionItemIds.Coins, 5) },
            standard: new[]
            {
                new WeightedDrop(RegionItemIds.BronzeDagger, 1, 1500),
                new WeightedDrop(RegionItemIds.BronzeBar, 1, 1500),
                // remaining ~7000/10000 weight is the "nothing" slice
            }));

    /// <summary>Cavern rodent: aggressive, high-evasion melee; drops bones for Prayer (Part 23.2).</summary>
    public static readonly MonsterArchetype Rat = new(
        "cavern_rodent", "Cavern Rodent", Level: 4, MaxHitpoints: 12, CombatStyle.Melee,
        Aggressive: true, AggroRadius: 4,
        new LootTable(guaranteed: new[] { new LootDrop(RegionItemIds.BrittleBones, 1) }));

    public static readonly IReadOnlyDictionary<string, MonsterArchetype> ById =
        new Dictionary<string, MonsterArchetype>
        {
            [Chicken.Id] = Chicken,
            [Skeleton.Id] = Skeleton,
            [Goblin.Id] = Goblin,
            [Rat.Id] = Rat,
        };
}
