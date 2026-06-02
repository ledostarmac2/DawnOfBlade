using System.Linq;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.RiverValley;
using Xunit;

namespace DawnOfBlade.Tests;

public class RiverValleyRegionTests
{
    private readonly RiverValleyRegion _region = new();

    [Fact]
    public void RespawnTile_IsCastleCourtyardZeroPoint()
    {
        Assert.Equal(new GridCoordinate(35, 35), _region.RespawnTile);
        Assert.Equal(new Godot.Vector3(0, 0, 0), _region.TileToWorld(_region.RespawnTile));
    }

    [Fact]
    public void RiverBlocksCrossingExceptAtReinforcedBridge()
    {
        Assert.False(_region.IsWalkable(new GridCoordinate(57, 34)));
        Assert.True(_region.IsWalkable(new GridCoordinate(57, 35)));
        Assert.False(_region.IsWalkable(new GridCoordinate(57, 36)));
    }

    [Fact]
    public void CastlePerimeterLeavesSouthernGatePassable()
    {
        Assert.False(_region.IsWalkable(new GridCoordinate(34, 20)));
        Assert.True(_region.IsWalkable(new GridCoordinate(35, 20)));
        Assert.False(_region.IsWalkable(new GridCoordinate(36, 20)));
    }

    [Fact]
    public void StartingSkillLoopAnchorsMatchBlueprintCoordinates()
    {
        AssertAnchor("copper_ore_01", 85, 15, "mining");
        AssertAnchor("tin_ore_01", 95, 16, "mining");
        AssertAnchor("softwood_tree_01", 70, 60, "woodcutting");
        AssertAnchor("castle_hearth", 45, 35, "cooking");
        AssertAnchor("castle_spinning_wheel", 25, 40, "crafting");
    }

    [Fact]
    public void RegionDefinesFullResourceAndEcosystemBudgets()
    {
        Assert.Equal(6, _region.Anchors.Count(anchor => anchor.Id.StartsWith("copper_ore_")));
        Assert.Equal(6, _region.Anchors.Count(anchor => anchor.Id.StartsWith("tin_ore_")));
        Assert.Equal(14, _region.Anchors.Count(anchor => anchor.Id.StartsWith("softwood_tree_")));
        Assert.Equal(4, _region.SpawnPools.Count);
        Assert.Equal(18, _region.SpawnPools.Single(pool => pool.Id == "woodland_marauders").MaximumActive);
    }

    [Fact]
    public void ResourceTilesAreServerPathingObstacles()
    {
        var copper = _region.Anchors.Single(anchor => anchor.Id == "copper_ore_01");

        Assert.True(copper.BlocksMovement);
        Assert.False(_region.IsWalkable(copper.Coordinate));
    }

    private void AssertAnchor(string id, int x, int z, string interactionType)
    {
        var anchor = _region.Anchors.Single(anchor => anchor.Id == id);
        Assert.Equal(new GridCoordinate(x, z), anchor.Coordinate);
        Assert.Equal(interactionType, anchor.InteractionType);
    }
}
