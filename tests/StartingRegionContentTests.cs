using System;
using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Combat;
using DawnOfBlade.GameSystems;
using DawnOfBlade.GameSystems.Content;
using DawnOfBlade.World.Grid;
using Xunit;

namespace DawnOfBlade.Tests;

public class StartingRegionContentTests
{
    private sealed class ScriptedRandom : IRandomSource
    {
        private readonly Queue<int> _ints;
        public ScriptedRandom(params int[] ints) => _ints = new Queue<int>(ints);
        public double NextDouble() => 0;
        public int Next(int maxExclusive) => _ints.Dequeue();
        public int Next(int minInclusive, int maxExclusive) => _ints.Dequeue();
    }

    // ---- Part 25 interaction map -----------------------------------------

    [Fact]
    public void StartingRegion_RespawnPointAndBridgeMatchBlueprint()
    {
        Assert.Equal(new GridCoordinate(35, 35), StartingRegion.RespawnPoint);
        Assert.Equal(new GridCoordinate(51, 35), StartingRegion.BridgeWest);
        Assert.Equal(new GridCoordinate(60, 35), StartingRegion.BridgeEast);
        Assert.Equal(128, StartingRegion.RegionSize);
        Assert.True(StartingRegion.IsWesternSafeSector(new GridCoordinate(35, 35)));
        Assert.False(StartingRegion.IsWesternSafeSector(new GridCoordinate(85, 15)));
    }

    [Fact]
    public void InteractionNodes_MatchPart25Coordinates()
    {
        var nodes = StartingRegion.InteractionNodes;
        Assert.Equal(8, nodes.Count);

        var copper1 = nodes.Single(n => n.NodeId == "copper_ore_01");
        Assert.Equal(new GridCoordinate(85, 15), copper1.Coordinate);
        Assert.Equal(InteractionType.Mining, copper1.Interaction);
        Assert.Equal(1, copper1.RequiredLevel);
        Assert.Equal(RegionItemIds.CopperOre, copper1.ItemId);
        Assert.False(copper1.IsProcessingStation);

        Assert.Equal(4, StartingRegion.NodesOfType(InteractionType.Mining).Count());
        Assert.Equal(2, StartingRegion.NodesOfType(InteractionType.Woodcutting).Count());

        // Processing stations carry no yield item.
        var hearth = nodes.Single(n => n.NodeId == "castle_cooking_hearth");
        Assert.True(hearth.IsProcessingStation);
        Assert.Equal(new GridCoordinate(45, 35), hearth.Coordinate);
        Assert.Equal(new GridCoordinate(25, 40), nodes.Single(n => n.NodeId == "castle_spinning_wheel").Coordinate);
    }

    [Fact]
    public void ResourceSpawnerPool_SeedsOnlyHarvestableTiles()
    {
        var pool = StartingRegion.CreateResourceSpawnerPool();
        Assert.Equal(6, pool.ActiveCount); // 4 ore + 2 trees, no processing stations
        Assert.True(pool.IsActive(new GridCoordinate(85, 15)));
        Assert.False(pool.IsActive(new GridCoordinate(45, 35))); // hearth not a resource
    }

    [Fact]
    public void BronzeRecipe_CombinesCopperAndTin()
    {
        var recipe = StartingRegion.BronzeRecipe;
        Assert.Equal(RegionItemIds.BronzeBar, recipe.OutputItemId);
        Assert.Equal(1, recipe.RequiredSkillLevel);
        Assert.Equal(1, recipe.Ingredients[RegionItemIds.CopperOre]);
        Assert.Equal(1, recipe.Ingredients[RegionItemIds.TinOre]);
    }

    // ---- Parts 23: monster loot ------------------------------------------

    [Fact]
    public void Chicken_DropsGuaranteedFeathersAndPoultry()
    {
        var drops = LootRoller.Roll(StartingRegionMonsters.Chicken.Loot, new ScriptedRandom());
        Assert.Contains(drops, d => d.ItemId == RegionItemIds.Feathers);
        Assert.Contains(drops, d => d.ItemId == RegionItemIds.RawPoultry);
        Assert.Equal(2, drops.Count);
    }

    [Fact]
    public void Skeleton_DropsGuaranteedBrittleBones()
    {
        var drops = LootRoller.Roll(StartingRegionMonsters.Skeleton.Loot, new ScriptedRandom());
        Assert.Single(drops);
        Assert.Equal(RegionItemIds.BrittleBones, drops[0].ItemId);
    }

    [Fact]
    public void Goblin_AlwaysDropsCoins_AndCanDropBronzeFromStandardPool()
    {
        var goblin = StartingRegionMonsters.Goblin;

        // Standard roll of 1 -> first standard entry (Bronze Dagger) within its 1..1500 band.
        var withBronze = LootRoller.Roll(goblin.Loot, new ScriptedRandom(1));
        Assert.Contains(withBronze, d => d.ItemId == RegionItemIds.Coins);
        Assert.Contains(withBronze, d => d.ItemId == RegionItemIds.BronzeDagger);

        // Standard roll of 9999 -> beyond the 3000 weighted band -> coins only.
        var coinsOnly = LootRoller.Roll(goblin.Loot, new ScriptedRandom(9999));
        Assert.Single(coinsOnly);
        Assert.Equal(RegionItemIds.Coins, coinsOnly[0].ItemId);
    }

    [Fact]
    public void MonsterRegistry_ContainsAllFourArchetypes()
    {
        Assert.Equal(4, StartingRegionMonsters.ById.Count);
        Assert.True(StartingRegionMonsters.ById["forest_marauder"].Aggressive);
        Assert.Equal(3, StartingRegionMonsters.ById["forest_marauder"].AggroRadius);
        Assert.False(StartingRegionMonsters.ById["chicken"].Aggressive);
        Assert.Equal(CombatStyle.Melee, StartingRegionMonsters.ById["skeleton"].Style);
    }
}
