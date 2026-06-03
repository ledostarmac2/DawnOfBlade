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
    [Export] public string SpeakerId { get; set; } = "";
    [Export] public string Role { get; set; } = "";
    [Export] public string DialogueRootId { get; set; } = "mira_intro";
    [Export] public int Seed { get; set; } = 1;
    [Export] public bool UseAuthoredAppearance { get; set; }

    [Export] public string SkinTone { get; set; } = "#e0b48c";
    [Export] public string HairColor { get; set; } = "#3a2a1a";
    [Export] public string ShirtColor { get; set; } = "#6a5acd";
    [Export] public string LegColor { get; set; } = "#3b3b46";
    [Export] public string FootColor { get; set; } = "#4a3324";
    [Export] public string Presentation { get; set; } = "masculine";
    [Export] public string BodyType { get; set; } = "slim";
    [Export] public int HairStyle { get; set; }
    [Export] public int HeadStyle { get; set; }

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
        if (UseAuthoredAppearance)
        {
            DisplayName = string.IsNullOrWhiteSpace(Role) ? SpeakerName : $"{SpeakerName} the {Role}";
            Tint(new Appearance
            {
                Presentation = Presentation,
                BodyType = BodyType,
                HeadStyle = HeadStyle,
                HairStyle = HairStyle,
                SkinTone = SkinTone,
                HairColor = HairColor,
                ShirtColor = ShirtColor,
                LegColor = LegColor,
                FootColor = FootColor,
            });
            return;
        }

        var npc = new NpcRandomizer().Generate(Seed, DialogueRootId);
        SpeakerName = npc.Name;
        Role = npc.Role;
        DisplayName = $"{npc.Name} the {npc.Role}";
        Tint(npc.Appearance);
    }

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.ShowNpcDialogue(
            string.IsNullOrWhiteSpace(SpeakerId) ? DialogueRootId : SpeakerId);
    }

    private void Tint(Appearance appearance)
    {
        if (GetNodeOrNull<HumanoidVisual>("Humanoid") is { } humanoid)
        {
            humanoid.Apply(appearance);
        }
    }
}
