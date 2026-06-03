using System.Linq;
using DawnOfBlade.Combat;
using DawnOfBlade.Engine.Ai;
using DawnOfBlade.Engine.Progression;
using DawnOfBlade.Engine.Spatial;
using Xunit;

namespace DawnOfBlade.Tests;

public class CombatLevelTests
{
    [Fact]
    public void Compute_FloorsAtOne()
    {
        Assert.Equal(1, CombatLevel.Compute(0, 0, 0, 0));
        Assert.True(CombatLevel.Compute(1, 1, 1, 1) >= 1);
    }

    [Fact]
    public void Compute_MaxedMeleeProfileIs113()
    {
        Assert.Equal(113, CombatLevel.Compute(99, 99, 99, 99));
    }

    [Fact]
    public void Compute_RisesWithEachSkill()
    {
        var baseline = CombatLevel.Compute(10, 10, 10, 10);
        Assert.True(CombatLevel.Compute(20, 10, 10, 10) > baseline);
        Assert.True(CombatLevel.Compute(10, 20, 10, 10) > baseline);
        Assert.True(CombatLevel.Compute(10, 10, 20, 10) > baseline);
        Assert.True(CombatLevel.Compute(10, 10, 10, 20) > baseline);
    }
}

public class AggressionPolicyTests
{
    [Theory]
    [InlineData(MonsterArchetype.Passive)]
    [InlineData(MonsterArchetype.Defensive)]
    public void NonInitiators_NeverEngage(MonsterArchetype archetype)
    {
        Assert.False(AggressionPolicy.WillEngage(archetype, selfCombatLevel: 50, targetCombatLevel: 1, distanceTiles: 1, aggroRadius: 8));
    }

    [Fact]
    public void Aggressive_EngagesWeakerTargetInRange()
    {
        Assert.True(AggressionPolicy.WillEngage(MonsterArchetype.Aggressive, 10, 5, distanceTiles: 4, aggroRadius: 6));
    }

    [Fact]
    public void Aggressive_IgnoresTargetThatOutgrewIt()
    {
        // level 10 monster: tolerance = 2*10 + 1 = 21. A combat-22 target is ignored.
        Assert.False(AggressionPolicy.WillEngage(MonsterArchetype.Aggressive, 10, 22, distanceTiles: 1, aggroRadius: 6));
        Assert.True(AggressionPolicy.WillEngage(MonsterArchetype.Aggressive, 10, 21, distanceTiles: 1, aggroRadius: 6));
    }

    [Fact]
    public void Aggressive_IgnoresTargetOutOfRange()
    {
        Assert.False(AggressionPolicy.WillEngage(MonsterArchetype.Aggressive, 10, 1, distanceTiles: 7, aggroRadius: 6));
    }

    [Fact]
    public void Predator_EngagesRegardlessOfLevel()
    {
        Assert.True(AggressionPolicy.WillEngage(MonsterArchetype.Predator, 10, 99, distanceTiles: 6, aggroRadius: 6));
    }
}

public class PathfinderAdjacencyTests
{
    [Fact]
    public void FindPathAdjacent_StopsNextToTargetWithoutSteppingOnIt()
    {
        var grid = new CollisionGrid(10, 10);
        var path = GridPathfinder.FindPathAdjacent(grid, new TrueTile(0, 0), new TrueTile(5, 0));

        Assert.NotEmpty(path);
        Assert.Equal(1, path[^1].ManhattanDistance(new TrueTile(5, 0)));
        Assert.DoesNotContain(new TrueTile(5, 0), path);
    }

    [Fact]
    public void FindPathAdjacent_ReturnsEmptyWhenAlreadyAdjacent()
    {
        var grid = new CollisionGrid(10, 10);
        Assert.Empty(GridPathfinder.FindPathAdjacent(grid, new TrueTile(4, 0), new TrueTile(5, 0)));
    }

    [Fact]
    public void FindPath_RespectsExpansionBudget()
    {
        var grid = new CollisionGrid(40, 40);
        // A budget of 1 cannot reach a far tile, so the path is abandoned.
        Assert.Empty(GridPathfinder.FindPath(grid, new TrueTile(0, 0), new TrueTile(30, 30), maxExpansion: 1));
        // With an ample budget the same query succeeds.
        Assert.NotEmpty(GridPathfinder.FindPath(grid, new TrueTile(0, 0), new TrueTile(30, 30)));
    }
}

public class ActorBrainTests
{
    private static ActorBrain NewBrain(
        MonsterArchetype archetype,
        int combatLevel,
        TrueTile anchor,
        int wanderRadius,
        int leashRadius,
        int aggroRadius,
        int seed = 1)
    {
        var area = new WanderArea(anchor, wanderRadius, leashRadius);
        var options = new ActorBrainOptions { AggroRadius = aggroRadius, MinIdleTicks = 1, MaxIdleTicks = 3 };
        return new ActorBrain(area, archetype, combatLevel, options, new SystemRandomSource(seed));
    }

    [Fact]
    public void Passive_NeverEngages_AndStaysInsideItsArea()
    {
        var anchor = new TrueTile(10, 10);
        var grid = new CollisionGrid(40, 40);
        var brain = NewBrain(MonsterArchetype.Passive, combatLevel: 5, anchor, wanderRadius: 3, leashRadius: 6, aggroRadius: 0);

        // A target sits right next to it the whole time; a passive actor must never react.
        var perception = Perception.Of(new TrueTile(10, 11), combatLevel: 1);
        var moved = false;

        for (var tick = 0; tick < 300; tick++)
        {
            var step = brain.Tick(grid, perception);
            Assert.NotEqual(AiState.Chasing, step.State);
            Assert.True(anchor.ChebyshevDistance(step.Position) <= 3, $"left area at {step.Position}");
            moved |= step.Moved;
        }

        Assert.True(moved, "a wandering actor should move at least once");
    }

    [Fact]
    public void Aggressive_ChasesWeakTargetIntoAttackRange()
    {
        var grid = new CollisionGrid(40, 40);
        var brain = NewBrain(MonsterArchetype.Aggressive, combatLevel: 10, new TrueTile(5, 5), wanderRadius: 3, leashRadius: 10, aggroRadius: 6);
        var target = new TrueTile(9, 5);
        var perception = Perception.Of(target, combatLevel: 5);

        var reached = false;
        for (var tick = 0; tick < 20 && !reached; tick++)
        {
            var step = brain.Tick(grid, perception);
            reached = step.InAttackRange;
            if (reached)
            {
                Assert.Equal(AiState.Chasing, step.State);
                Assert.True(step.Position.ManhattanDistance(target) <= 1);
            }
        }

        Assert.True(reached, "aggressive monster never reached its target");
    }

    [Fact]
    public void Aggressive_IgnoresTargetThatOutgrewIt()
    {
        var anchor = new TrueTile(5, 5);
        var grid = new CollisionGrid(40, 40);
        var brain = NewBrain(MonsterArchetype.Aggressive, combatLevel: 10, anchor, wanderRadius: 3, leashRadius: 10, aggroRadius: 6);
        var perception = Perception.Of(new TrueTile(7, 5), combatLevel: 60); // far above tolerance

        for (var tick = 0; tick < 100; tick++)
        {
            var step = brain.Tick(grid, perception);
            Assert.NotEqual(AiState.Chasing, step.State);
            Assert.True(anchor.ChebyshevDistance(step.Position) <= 3);
        }
    }

    [Fact]
    public void Predator_ChasesEvenAStrongerTarget()
    {
        var grid = new CollisionGrid(40, 40);
        var brain = NewBrain(MonsterArchetype.Predator, combatLevel: 10, new TrueTile(5, 5), wanderRadius: 3, leashRadius: 12, aggroRadius: 6);
        var target = new TrueTile(9, 5);
        var perception = Perception.Of(target, combatLevel: 99);

        var reached = false;
        for (var tick = 0; tick < 20 && !reached; tick++)
        {
            reached = brain.Tick(grid, perception).InAttackRange;
        }

        Assert.True(reached);
    }

    [Fact]
    public void Chase_LeashesAndReturnsHome()
    {
        const int leash = 5;
        var anchor = new TrueTile(5, 5);
        var grid = new CollisionGrid(60, 60);
        // High level so the disparity rule never ends the chase; only the leash should.
        var brain = NewBrain(MonsterArchetype.Aggressive, combatLevel: 50, anchor, wanderRadius: 2, leashRadius: leash, aggroRadius: 6);

        // Phase 1: dangle the target two tiles east of the monster every tick so it keeps chasing
        // outward until it breaks its leash. Walking is 1 tile/tick, so it can overshoot by 1.
        var overshot = false;
        var maxDistanceFromAnchor = 0;
        for (var tick = 0; tick < 40 && !overshot; tick++)
        {
            var bait = new TrueTile(brain.Position.X + 2, 5);
            var step = brain.Tick(grid, Perception.Of(bait, combatLevel: 5));
            maxDistanceFromAnchor = System.Math.Max(maxDistanceFromAnchor, anchor.ChebyshevDistance(step.Position));
            overshot = anchor.ChebyshevDistance(step.Position) > leash;
        }

        Assert.True(overshot, "monster should have been lured past its leash");
        Assert.True(maxDistanceFromAnchor <= leash + 1, $"monster strayed to {maxDistanceFromAnchor} (leash {leash})");

        // Phase 2: with the target gone it must give up and walk all the way back to its anchor.
        var reachedAnchor = false;
        for (var tick = 0; tick < 80 && !reachedAnchor; tick++)
        {
            reachedAnchor = brain.Tick(grid, Perception.None).Position == anchor;
        }

        Assert.True(reachedAnchor, "monster never returned to its anchor");
    }
}
