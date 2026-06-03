using System.Linq;
using DawnOfBlade.Engine.Spatial;
using DawnOfBlade.Movement;
using Godot;
using Xunit;

namespace DawnOfBlade.Tests;

public class MovementTests
{
    [Fact]
    public void Walking_MovesOneTilePerTick()
    {
        var controller = new MovementController(new TrueTile(0, 0)) { Mode = MoveMode.Walking };
        controller.SetPath(new[] { new TrueTile(1, 0), new TrueTile(2, 0), new TrueTile(3, 0) });

        var first = controller.Step();

        Assert.True(first.Moved);
        Assert.Equal(new TrueTile(1, 0), first.Landing);
        Assert.Empty(first.SkippedTiles);
    }

    [Fact]
    public void Running_MovesTwoTilesAndSkipsTheIntermediate()
    {
        var controller = new MovementController(new TrueTile(0, 0)) { Mode = MoveMode.Running };
        controller.SetPath(new[] { new TrueTile(1, 0), new TrueTile(2, 0), new TrueTile(3, 0), new TrueTile(4, 0) });

        var step = controller.Step();

        Assert.Equal(new TrueTile(2, 0), step.Landing);
        Assert.Equal(new[] { new TrueTile(1, 0) }, step.SkippedTiles);
    }

    [Fact]
    public void Running_SkipsTrapsOnIntermediateTilesButSpringsThemOnLanding()
    {
        var field = new TileTriggerField();
        field.AddTrap(new TrueTile(1, 0)); // intermediate while running
        field.AddTrap(new TrueTile(2, 0)); // landing

        var controller = new MovementController(new TrueTile(0, 0)) { Mode = MoveMode.Running };
        controller.SetPath(new[] { new TrueTile(1, 0), new TrueTile(2, 0) });

        var sprung = field.Evaluate(controller.Step());

        Assert.Equal(new[] { new TrueTile(2, 0) }, sprung);
    }

    [Fact]
    public void Walking_SpringsTrapOnEveryTile()
    {
        var field = new TileTriggerField();
        field.AddTrap(new TrueTile(1, 0));

        var controller = new MovementController(new TrueTile(0, 0)) { Mode = MoveMode.Walking };
        controller.SetPath(new[] { new TrueTile(1, 0) });

        Assert.Single(field.Evaluate(controller.Step()));
    }

    [Fact]
    public void Projectile_PassesOverLowObstacleButNotSolid()
    {
        var lowGrid = new CollisionGrid(5, 5);
        lowGrid.SetLowObstacle(2, 0);
        Assert.True(LineOfSight.HasProjectilePath(lowGrid, new TrueTile(0, 0), new TrueTile(4, 0)));

        var solidGrid = new CollisionGrid(5, 5);
        solidGrid.SetSolid(2, 0);
        Assert.False(LineOfSight.HasProjectilePath(solidGrid, new TrueTile(0, 0), new TrueTile(4, 0)));
    }

    [Fact]
    public void Pathfinder_RoutesAroundSolidWall()
    {
        var grid = new CollisionGrid(5, 5);
        for (var y = 0; y < 4; y++)
        {
            grid.SetSolid(2, y); // wall blocking the direct route, with a gap at y = 4
        }

        var path = GridPathfinder.FindPath(grid, new TrueTile(0, 0), new TrueTile(4, 0));

        Assert.NotEmpty(path);
        Assert.Equal(new TrueTile(4, 0), path[^1]);
        Assert.DoesNotContain(path, tile => grid.BlocksMovement(tile));
    }

    [Fact]
    public void ClickToMove_FollowsQueuedWaypoints()
    {
        var controller = new ClickToMoveController { MoveSpeed = 1.0f, ArrivalDistance = 0.05f };
        controller.SetPath(new[] { new Vector3(1, 0, 0), new Vector3(2, 0, 0) });

        Assert.Equal(new Vector3(1, 0, 0), controller.TargetPosition);
        Assert.Equal(new Vector3(1, 0, 0), controller.GetVelocity(Vector3.Zero));
        Assert.Equal(new Vector3(1, 0, 0), controller.GetVelocity(new Vector3(1, 0, 0)));
        Assert.Equal(Vector3.Zero, controller.GetVelocity(new Vector3(2, 0, 0)));
        Assert.Null(controller.TargetPosition);
    }
}
