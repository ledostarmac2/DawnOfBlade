using DawnOfBlade.Characters;
using DawnOfBlade.Combat;
using DawnOfBlade.Core;
using DawnOfBlade.Engine.Ai;
using DawnOfBlade.Engine.Spatial;
using Godot;

namespace DawnOfBlade.Interaction;

/// <summary>
/// A villager NPC whose name and appearance are randomized from <see cref="Seed"/> via
/// <see cref="NpcRandomizer"/>, so each placed NPC looks and is named differently while staying
/// stable across runs. Interacting opens that NPC's dialogue.
/// </summary>
public partial class PrototypeNpc : Interactable
{
    [Export] public string SpeakerName { get; set; } = "Ari";
    [Export] public int Seed { get; set; } = 1;

    /// <summary>Chebyshev tiles this villager strolls from its spawn point.</summary>
    [Export] public int WanderRadius { get; set; } = 3;

    /// <summary>
    /// Builds a Passive behavior controller so villagers stroll their fixed area but never engage.
    /// Shares the exact same <see cref="ActorBrain"/> used by monsters; only the archetype differs.
    /// </summary>
    public ActorBrain BuildBrain(TrueTile anchor, IRandomSource random)
    {
        var area = new WanderArea(anchor, WanderRadius, WanderRadius + 2);
        var options = new ActorBrainOptions { AggroRadius = 0 };
        return new ActorBrain(area, MonsterArchetype.Passive, combatLevel: 3, options, random);
    }

    public override void _Ready()
    {
        var npc = new NpcRandomizer().Generate(Seed);
        SpeakerName = npc.Name;
        DisplayName = $"{npc.Name} the {npc.Role}";
        Tint(npc.Appearance);
    }

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.ShowNpcDialogue(SpeakerName);
    }

    private void Tint(Appearance appearance)
    {
        if (GetNodeOrNull<HumanoidVisual>("Humanoid") is { } humanoid)
        {
            humanoid.Apply(appearance);
        }
    }
}
