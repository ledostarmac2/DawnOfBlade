using DawnOfBlade.Combat;
using DawnOfBlade.Core;
using DawnOfBlade.Engine.Ai;
using DawnOfBlade.Engine.Spatial;
using DawnOfBlade.World;
using Godot;

namespace DawnOfBlade.Interaction;

/// <summary>
/// A clickable hostile test actor for the combat prototype. Clicking it routes an attack
/// through <see cref="GameManager"/>. When defeated it stops accepting interaction until the
/// game manager revives it.
/// </summary>
public partial class HostileActor : Interactable
{
    [Export] public int MaxHitpoints { get; set; } = 12;
    [Export] public int AttackLevel { get; set; } = 2;
    [Export] public int StrengthLevel { get; set; } = 3;
    [Export] public int DefenseLevel { get; set; } = 1;
    [Export] public string LootItemId { get; set; } = "coins";
    [Export] public int LootQuantity { get; set; } = 3;

    // ---- AI / movement configuration (consumed by ActorBrain) ----
    /// <summary>One of MonsterArchetype: Passive, Defensive, Aggressive, Predator.</summary>
    [Export] public string Archetype { get; set; } = "Aggressive";

    /// <summary>Chebyshev tiles this actor roams from its spawn while idle.</summary>
    [Export] public int WanderRadius { get; set; } = 4;

    /// <summary>Chebyshev tiles within which it notices a target to chase.</summary>
    [Export] public int AggroRadius { get; set; } = 6;

    /// <summary>Chebyshev tiles from spawn beyond which it abandons a chase and returns home.</summary>
    [Export] public int LeashRadius { get; set; } = 8;

    /// <summary>Whether it runs (2 tiles/tick) instead of walking while chasing.</summary>
    [Export] public bool RunWhileChasing { get; set; } = false;

    public CombatProfile Profile { get; private set; } = new(2, 3, 1, 12);

    /// <summary>The displayed level of this monster, derived from its combat stats.</summary>
    public int CombatLevel => Profile.CombatLevel;

    /// <summary>
    /// Builds the engine-pure behavior controller for this actor, anchored at its spawn tile.
    /// The scene's heartbeat should call <c>brain.Tick(grid, perception)</c> once per tick and
    /// move the node toward <c>brain.Position</c>. See <c>docs/AI_SYSTEMS.md</c>.
    /// </summary>
    public ActorBrain BuildBrain(TrueTile anchor, IRandomSource random)
    {
        var archetype = System.Enum.TryParse<MonsterArchetype>(Archetype, ignoreCase: true, out var parsed)
            ? parsed
            : MonsterArchetype.Aggressive;
        var area = new WanderArea(anchor, WanderRadius, LeashRadius);
        var options = new ActorBrainOptions { AggroRadius = AggroRadius, RunWhileChasing = RunWhileChasing };
        return new ActorBrain(area, archetype, Profile.CombatLevel, options, random);
    }

    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Humanoid") is null && GetNodeOrNull<Node3D>("GeneratedTrainingDummy") is null)
        {
            if (GetNodeOrNull<Node3D>("Mesh") is { } placeholder)
            {
                placeholder.Visible = false;
            }

            AddChild(GeneratedAssetFactory.CreateHostile("training_dummy", "#9b6b3f"));
        }

        ResetStats();
    }

    public void ResetStats()
    {
        Profile = new CombatProfile(AttackLevel, StrengthLevel, DefenseLevel, MaxHitpoints);
        Visible = true;
    }

    public override bool CanInteract(Node interactor) => !Profile.IsDefeated;

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.AttackHostile(this);
    }
}
