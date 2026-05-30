using DawnOfBlade.Combat;
using DawnOfBlade.Core;
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

    public CombatProfile Profile { get; private set; } = new(2, 3, 1, 12);

    public override void _Ready()
    {
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
