using System;
using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.GameSystems;
using DawnOfBlade.GameSystems.Content;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.RiverValley;
using Xunit;

namespace DawnOfBlade.Tests;

public class GameSystemsAdapterTests
{
    [Fact]
    public void LiveItemIdAdapter_StartingRegionBindingsRoundTrip()
    {
        var adapter = LiveItemIdAdapter.StartingRegion;

        Assert.All(adapter.Bindings, binding =>
        {
            Assert.Equal(binding.LiveItemId, adapter.ToLiveId(binding.GameItemId));
            Assert.Equal(binding.GameItemId, adapter.ToGameItemId(binding.LiveItemId));
            Assert.True(adapter.TryToLiveId(binding.GameItemId, out var liveItemId));
            Assert.Equal(binding.LiveItemId, liveItemId);
            Assert.True(adapter.TryToGameItemId(binding.LiveItemId, out var gameItemId));
            Assert.Equal(binding.GameItemId, gameItemId);
        });
    }

    [Fact]
    public void LiveItemIdAdapter_UnknownBindingsUseTryOrThrowStrictly()
    {
        var adapter = LiveItemIdAdapter.StartingRegion;

        Assert.False(adapter.TryToLiveId(-1, out _));
        Assert.False(adapter.TryToGameItemId("missing_item", out _));
        Assert.Throws<KeyNotFoundException>(() => adapter.ToLiveId(-1));
        Assert.Throws<KeyNotFoundException>(() => adapter.ToGameItemId("missing_item"));
    }

    [Fact]
    public void LiveItemIdAdapter_RejectsDuplicateBindings()
    {
        Assert.Throws<ArgumentException>(() => new LiveItemIdAdapter(new[]
        {
            new ItemIdBinding(1, "one"),
            new ItemIdBinding(1, "two"),
        }));
        Assert.Throws<ArgumentException>(() => new LiveItemIdAdapter(new[]
        {
            new ItemIdBinding(1, "same"),
            new ItemIdBinding(2, "same"),
        }));
    }

    [Fact]
    public void StartingRegion_AllHarvestAndLootItemsMapToLiveIds()
    {
        var adapter = LiveItemIdAdapter.StartingRegion;
        var harvestedItems = StartingRegion.InteractionNodes
            .Where(node => !node.IsProcessingStation)
            .Select(node => node.ItemId);
        var lootItems = StartingRegionMonsters.ById.Values.SelectMany(monster =>
            monster.Loot.GuaranteedDrops.Select(drop => drop.ItemId)
                .Concat(monster.Loot.StandardDrops.Select(drop => drop.ItemId))
                .Concat(monster.Loot.RareDrops.Select(drop => drop.ItemId)));

        Assert.All(harvestedItems.Concat(lootItems).Distinct(), itemId =>
            Assert.True(adapter.TryToLiveId(itemId, out _), $"Missing live mapping for item {itemId}."));
    }

    [Fact]
    public void StartingRegion_CanonicalHarvestMetadataMatchesLiveResourceIndex()
    {
        var harvestNodes = StartingRegion.InteractionNodes.Where(node => !node.IsProcessingStation).ToArray();
        var liveResourceAnchors = new RiverValleyRegion().Anchors
            .Where(anchor => anchor.Type == RegionAnchorType.Resource)
            .ToArray();

        Assert.Equal(26, harvestNodes.Length);
        Assert.Equal(harvestNodes.Length, harvestNodes.Select(node => node.NodeId).Distinct().Count());
        Assert.Equal(harvestNodes.Length, harvestNodes.Select(node => node.Coordinate).Distinct().Count());
        Assert.Equal(
            liveResourceAnchors.Select(anchor => (anchor.Id, anchor.Coordinate)).OrderBy(anchor => anchor.Id),
            harvestNodes.Select(node => (node.NodeId, node.Coordinate)).OrderBy(node => node.NodeId));
        Assert.True(StartingRegion.TryGetNode("copper_ore_01", out var copper));
        Assert.Same(copper, StartingRegion.GetNode("copper_ore_01"));
        Assert.Throws<KeyNotFoundException>(() => StartingRegion.GetNode("missing_node"));
    }

    [Fact]
    public void ResourceSpawnerPool_RespawnsStableNodeIdAndSupportsCoordinateLookup()
    {
        var coordinate = new GridCoordinate(85, 15);
        var pool = new ResourceSpawnerPool(new[] { new ResourceSpawnAnchor("copper_ore_01", coordinate) });

        Assert.True(pool.TryGetAnchor("copper_ore_01", out var byId));
        Assert.True(pool.TryGetByCoordinate(coordinate, out var byCoordinate));
        Assert.Equal(byId, byCoordinate);

        pool.Deplete("copper_ore_01", currentTick: 10, respawnDelayTicks: 5);
        Assert.False(pool.IsActive("copper_ore_01"));
        Assert.Empty(pool.Tick(14));
        Assert.Equal(new[] { byId }, pool.Tick(15));
        Assert.True(pool.IsActive("copper_ore_01"));
    }

    [Fact]
    public void ResourceSpawnerPool_RejectsDuplicateNodeIdsAndCoordinates()
    {
        var coordinate = new GridCoordinate(1, 2);
        Assert.Throws<ArgumentException>(() => new ResourceSpawnerPool(new[]
        {
            new ResourceSpawnAnchor("node", coordinate),
            new ResourceSpawnAnchor("node", new GridCoordinate(2, 3)),
        }));
        Assert.Throws<ArgumentException>(() => new ResourceSpawnerPool(new[]
        {
            new ResourceSpawnAnchor("node-a", coordinate),
            new ResourceSpawnAnchor("node-b", coordinate),
        }));
    }

    [Fact]
    public void RiverValleyHostileSpawnIdsResolveToStartingRegionArchetypes()
    {
        var region = new RiverValleyRegion();

        Assert.All(region.SpawnPools, pool => Assert.True(
            StartingRegionMonsters.ById.ContainsKey(pool.EntityId),
            $"Missing hostile archetype for live spawn id '{pool.EntityId}'."));
    }
}
