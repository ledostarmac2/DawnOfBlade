using System.Linq;
using DawnOfBlade.World.Grid;
using Xunit;

namespace DawnOfBlade.Tests;

public class GridWorldTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(31, 0)]
    [InlineData(32, 1)]
    [InlineData(-1, -1)]
    [InlineData(-32, -1)]
    [InlineData(-33, -2)]
    public void ChunkCoordinate_UsesFloorDivisionAtBoundaries(int tileX, int expectedChunkX)
    {
        Assert.Equal(expectedChunkX, new GridCoordinate(tileX, 0).ToChunk().X);
    }

    [Fact]
    public void ChunkInterestManager_DefaultWindowCoversThreeByThreeChunks()
    {
        var interest = new ChunkInterestManager();
        var observer = new GridCoordinate(1, 1);

        Assert.True(interest.IsRelevant(observer, new GridCoordinate(63, 63)));
        Assert.False(interest.IsRelevant(observer, new GridCoordinate(64, 1)));
    }

    [Fact]
    public void ChunkInterestManager_FiltersSubjectsInStableInputOrder()
    {
        var interest = new ChunkInterestManager();
        var observer = new GridCoordinate(32, 32);
        var subjects = new[]
        {
            new Subject("near-a", new GridCoordinate(0, 0)),
            new Subject("far", new GridCoordinate(128, 128)),
            new Subject("near-b", new GridCoordinate(95, 95)),
        };

        Assert.Equal(new[] { "near-a", "near-b" },
            interest.FilterRelevant(observer, subjects, subject => subject.Position).Select(subject => subject.Id));
    }

    [Fact]
    public void WorldZones_ExpressIncreasingRisk()
    {
        Assert.True(WorldZone.VerdantValley.IsSafeZone);
        Assert.Equal(PlayerVersusPlayerRule.Localized, WorldZone.SunscorchedDunes.PlayerVersusPlayer);
        Assert.Equal(DeathDropRule.DropAllCarriedAndEquipped, WorldZone.WhisperingMire.DeathDrops);
    }

    [Fact]
    public void TileProfile_CarriesPathingAndLineOfSightRules()
    {
        var wall = new TileProfile(7, IsWalkable: false, IsLineOfSightBlocker: true);

        Assert.False(wall.IsWalkable);
        Assert.True(wall.IsLineOfSightBlocker);
    }

    [Fact]
    public void GridPathfinder_RoutesAroundStaticObstacles()
    {
        var blocked = new HashSet<GridCoordinate> { new(1, 0) };
        var pathfinder = new GridPathfinder(tile => !blocked.Contains(tile));

        var path = pathfinder.FindPath(new GridCoordinate(0, 0), new GridCoordinate(2, 0));

        Assert.Equal(new[]
        {
            new GridCoordinate(0, -1),
            new GridCoordinate(1, -1),
            new GridCoordinate(2, -1),
            new GridCoordinate(2, 0),
        }, path);
    }

    [Fact]
    public void GridPathfinder_ReturnsNoRouteToBlockedDestination()
    {
        var destination = new GridCoordinate(1, 0);
        var pathfinder = new GridPathfinder(tile => tile != destination);

        Assert.Empty(pathfinder.FindPath(new GridCoordinate(0, 0), destination));
    }

    [Fact]
    public void GridPathfinder_ExcludesStartingTileFromSteps()
    {
        var pathfinder = new GridPathfinder(_ => true);

        Assert.Equal(new[] { new GridCoordinate(1, 0), new GridCoordinate(2, 0) },
            pathfinder.FindPath(new GridCoordinate(0, 0), new GridCoordinate(2, 0)));
    }

    private sealed record Subject(string Id, GridCoordinate Position);
}
