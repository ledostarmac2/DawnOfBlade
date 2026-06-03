using System;
using DawnOfBlade.Combat;
using DawnOfBlade.Engine.Spatial;

namespace DawnOfBlade.Engine.Ai;

/// <summary>The behavioral state an actor is in at the end of a tick.</summary>
public enum AiState
{
    /// <summary>Standing still between wanders.</summary>
    Idle,

    /// <summary>Walking toward a random tile inside its wander area.</summary>
    Wandering,

    /// <summary>Pursuing a target via re-pathed melee approach.</summary>
    Chasing,

    /// <summary>Walking back to its spawn anchor after a chase ended or leashed out.</summary>
    Returning,
}

/// <summary>What the actor can sense about a potential target this tick (engine-supplied).</summary>
/// <param name="HasTarget">False when no candidate target is in the world / known to the actor.</param>
/// <param name="TargetTile">The target's authoritative tile.</param>
/// <param name="TargetCombatLevel">The target's combat level, for the disparity rule.</param>
public readonly record struct Perception(bool HasTarget, TrueTile TargetTile, int TargetCombatLevel)
{
    public static Perception None => new(false, default, 0);

    public static Perception Of(TrueTile tile, int combatLevel) => new(true, tile, combatLevel);
}

/// <summary>The result of advancing one tick of AI.</summary>
/// <param name="Position">The actor's tile after this tick.</param>
/// <param name="State">The behavioral state the actor ended the tick in.</param>
/// <param name="Moved">Whether the actor changed tiles this tick.</param>
/// <param name="InAttackRange">True when a chaser is adjacent to its target and should attack.</param>
/// <param name="Target">The tile being chased, or null when not chasing.</param>
public readonly record struct BrainStep(TrueTile Position, AiState State, bool Moved, bool InAttackRange, TrueTile? Target);

/// <summary>Tunable knobs for an <see cref="ActorBrain"/>.</summary>
public sealed class ActorBrainOptions
{
    /// <summary>Chebyshev radius within which an aggressive actor notices a target.</summary>
    public int AggroRadius { get; init; } = 6;

    /// <summary>Minimum ticks an actor idles before picking a new wander destination.</summary>
    public int MinIdleTicks { get; init; } = 2;

    /// <summary>Maximum ticks an actor idles before picking a new wander destination.</summary>
    public int MaxIdleTicks { get; init; } = 6;

    /// <summary>Whether a chaser runs (2 tiles/tick) instead of walking while pursuing.</summary>
    public bool RunWhileChasing { get; init; } = false;

    /// <summary>How many random destinations to try before giving up and idling another cycle.</summary>
    public int MaxWanderAttempts { get; init; } = 6;
}

/// <summary>
/// A deterministic, engine-pure behavior controller shared by every wandering actor — monsters and
/// town NPCs alike. Each tick it: (1) decides whether to engage a perceived target using the
/// actor's <see cref="MonsterArchetype"/> and the combat-level disparity rule
/// (<see cref="AggressionPolicy"/>); (2) chases via adjacency pathing while still leashed to its
/// <see cref="WanderArea"/>; (3) otherwise returns home or wanders inside its territory. A
/// <see cref="MonsterArchetype.Passive"/> actor never engages, so the same class drives villager
/// wandering for free.
/// <para>All randomness flows through an <see cref="IRandomSource"/> so behavior is reproducible.</para>
/// </summary>
public sealed class ActorBrain
{
    private readonly MovementController _mover;
    private readonly IRandomSource _random;
    private readonly WanderArea _area;
    private readonly ActorBrainOptions _options;
    private readonly int _searchBudget;

    private int _idleTicksRemaining;

    public ActorBrain(
        WanderArea area,
        MonsterArchetype archetype,
        int combatLevel,
        ActorBrainOptions options,
        IRandomSource random)
    {
        _area = area;
        Archetype = archetype;
        CombatLevel = Math.Max(1, combatLevel);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _mover = new MovementController(area.Anchor);

        var reach = Math.Max(area.LeashRadius, Math.Max(area.WanderRadius, options.AggroRadius));
        var span = (2 * reach) + 3;
        _searchBudget = span * span;

        State = AiState.Idle;
        BeginIdle();
    }

    public MonsterArchetype Archetype { get; }

    public int CombatLevel { get; }

    public TrueTile Position => _mover.Position;

    public TrueTile Anchor => _area.Anchor;

    public AiState State { get; private set; }

    /// <summary>Advances one tick of behavior against the world and the actor's perception.</summary>
    public BrainStep Tick(CollisionGrid grid, in Perception perception)
    {
        // A returning actor ignores aggro until it is home again, so it can't be yo-yoed in place
        // at the edge of its leash. Every other state re-evaluates engagement each tick.
        if (State != AiState.Returning && WantsToEngage(perception))
        {
            return Chase(grid, perception.TargetTile);
        }

        return State switch
        {
            AiState.Chasing => Return(grid),    // target lost / out of reach -> head home
            AiState.Returning => Return(grid),
            AiState.Wandering => Wander(grid),
            _ => Idle(grid),
        };
    }

    private bool WantsToEngage(in Perception perception)
    {
        if (!perception.HasTarget || !_area.WithinLeash(_mover.Position))
        {
            return false;
        }

        var distance = _mover.Position.ChebyshevDistance(perception.TargetTile);
        return AggressionPolicy.WillEngage(
            Archetype, CombatLevel, perception.TargetCombatLevel, distance, _options.AggroRadius);
    }

    private BrainStep Chase(CollisionGrid grid, TrueTile target)
    {
        State = AiState.Chasing;

        // Strayed past the leash mid-chase: abandon and go home.
        if (!_area.WithinLeash(_mover.Position))
        {
            return Return(grid);
        }

        // Already adjacent: hold position and signal the combat layer to attack.
        if (_mover.Position.ManhattanDistance(target) <= 1)
        {
            _mover.Stop();
            return new BrainStep(_mover.Position, AiState.Chasing, Moved: false, InAttackRange: true, target);
        }

        var path = GridPathfinder.FindPathAdjacent(grid, _mover.Position, target, _searchBudget);
        if (path.Count == 0)
        {
            _mover.Stop();
            return new BrainStep(_mover.Position, AiState.Chasing, Moved: false, InAttackRange: false, target);
        }

        _mover.Mode = _options.RunWhileChasing ? MoveMode.Running : MoveMode.Walking;
        _mover.SetPath(path);
        var moved = _mover.Step();
        var inRange = _mover.Position.ManhattanDistance(target) <= 1;
        return new BrainStep(_mover.Position, AiState.Chasing, moved.Moved, inRange, target);
    }

    private BrainStep Return(CollisionGrid grid)
    {
        State = AiState.Returning;

        if (_mover.Position == _area.Anchor)
        {
            BeginIdle();
            return new BrainStep(_mover.Position, AiState.Idle, Moved: false, InAttackRange: false, null);
        }

        var path = GridPathfinder.FindPath(grid, _mover.Position, _area.Anchor, _searchBudget);
        if (path.Count == 0)
        {
            // No route home (anchor walled off or out of budget): settle and idle where we are.
            BeginIdle();
            return new BrainStep(_mover.Position, AiState.Idle, Moved: false, InAttackRange: false, null);
        }

        _mover.Mode = MoveMode.Walking;
        _mover.SetPath(path);
        var moved = _mover.Step();

        if (_mover.Position == _area.Anchor)
        {
            BeginIdle();
            return new BrainStep(_mover.Position, AiState.Idle, moved.Moved, InAttackRange: false, null);
        }

        return new BrainStep(_mover.Position, AiState.Returning, moved.Moved, InAttackRange: false, null);
    }

    private BrainStep Idle(CollisionGrid grid)
    {
        State = AiState.Idle;

        if (_idleTicksRemaining > 0)
        {
            _idleTicksRemaining--;
            return new BrainStep(_mover.Position, AiState.Idle, Moved: false, InAttackRange: false, null);
        }

        if (TryStartWander(grid))
        {
            return Wander(grid);
        }

        BeginIdle();
        return new BrainStep(_mover.Position, AiState.Idle, Moved: false, InAttackRange: false, null);
    }

    private BrainStep Wander(CollisionGrid grid)
    {
        if (!_mover.HasPath)
        {
            BeginIdle();
            return new BrainStep(_mover.Position, AiState.Idle, Moved: false, InAttackRange: false, null);
        }

        State = AiState.Wandering;
        _mover.Mode = MoveMode.Walking;
        var moved = _mover.Step();

        if (!_mover.HasPath)
        {
            // Arrived at the wander destination this tick; rest before the next roam.
            BeginIdle();
            return new BrainStep(_mover.Position, AiState.Idle, moved.Moved, InAttackRange: false, null);
        }

        return new BrainStep(_mover.Position, AiState.Wandering, moved.Moved, InAttackRange: false, null);
    }

    private bool TryStartWander(CollisionGrid grid)
    {
        for (var attempt = 0; attempt < _options.MaxWanderAttempts; attempt++)
        {
            var dx = _random.Next(-_area.WanderRadius, _area.WanderRadius + 1);
            var dy = _random.Next(-_area.WanderRadius, _area.WanderRadius + 1);
            var destination = new TrueTile(_area.Anchor.X + dx, _area.Anchor.Y + dy);

            if (destination == _mover.Position || grid.BlocksMovement(destination))
            {
                continue;
            }

            var path = GridPathfinder.FindPath(grid, _mover.Position, destination, _searchBudget);
            if (path.Count == 0)
            {
                continue;
            }

            _mover.SetPath(path);
            return true;
        }

        return false;
    }

    private void BeginIdle()
    {
        State = AiState.Idle;
        _mover.Stop();
        var low = _options.MinIdleTicks;
        var high = Math.Max(low, _options.MaxIdleTicks);
        _idleTicksRemaining = _random.Next(low, high + 1);
    }
}
