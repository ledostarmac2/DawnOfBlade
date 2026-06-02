using System.Linq;
using DawnOfBlade.Combat;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.GroundItems;
using Xunit;

namespace DawnOfBlade.Tests;

public class AdvancedWorldRulesTests
{
    [Fact]
    public void GridLineOfSight_RasterizesAndRejectsInteriorBlocker()
    {
        var blocker = new GridCoordinate(2, 1);
        var lineOfSight = new GridLineOfSight(tile => tile == blocker);

        Assert.Equal(new[]
        {
            new GridCoordinate(0, 0),
            new GridCoordinate(1, 0),
            new GridCoordinate(2, 1),
            new GridCoordinate(3, 1),
        }, GridLineOfSight.Rasterize(new GridCoordinate(0, 0), new GridCoordinate(3, 1)).ToArray());
        Assert.False(lineOfSight.HasClearPath(new GridCoordinate(0, 0), new GridCoordinate(3, 1)));
    }

    [Fact]
    public void GridMovementRules_RejectsDiagonalCornerCutWhenBothFlanksAreBlocked()
    {
        var blocked = new HashSet<GridCoordinate> { new(1, 0), new(0, 1) };
        var rules = new GridMovementRules(tile => !blocked.Contains(tile));

        Assert.False(rules.CanStep(new GridCoordinate(0, 0), new GridCoordinate(1, 1)));
    }

    [Fact]
    public void GridMovementRules_AllowsDiagonalWhenOneFlankIsOpen()
    {
        var blocked = new HashSet<GridCoordinate> { new(1, 0) };
        var rules = new GridMovementRules(tile => !blocked.Contains(tile));

        Assert.True(rules.CanStep(new GridCoordinate(0, 0), new GridCoordinate(1, 1)));
    }

    [Fact]
    public void GroundItem_TransitionsFromPrivateToPublicToExpired()
    {
        var item = new GroundItem("drop-1", "logs", 3, new GridCoordinate(2, 4), "alice", 500);

        Assert.Equal(GroundItemPhase.Private, item.PhaseAt(599));
        Assert.True(item.IsVisibleTo("alice", 599));
        Assert.False(item.IsVisibleTo("bob", 599));
        Assert.Equal(GroundItemPhase.Public, item.PhaseAt(600));
        Assert.True(item.IsVisibleTo("bob", 600));
        Assert.Equal(GroundItemPhase.Expired, item.PhaseAt(700));
        Assert.False(item.IsVisibleTo("alice", 700));
    }

    [Fact]
    public void AggressorStatus_ExpiresAfterExactlyTwoThousandTicks()
    {
        var status = new AggressorStatus();

        status.ApplyAt(40);

        Assert.True(status.IsActive(2039));
        Assert.False(status.IsActive(2040));
    }
}
