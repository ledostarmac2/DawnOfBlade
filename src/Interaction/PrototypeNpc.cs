using DawnOfBlade.Characters;
using DawnOfBlade.Core;
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
        if (GetNodeOrNull<MeshInstance3D>("Mesh") is not { } mesh)
        {
            return;
        }

        var material = new StandardMaterial3D { AlbedoColor = new Color(appearance.ShirtColor) };
        mesh.SetSurfaceOverrideMaterial(0, material);
    }
}
