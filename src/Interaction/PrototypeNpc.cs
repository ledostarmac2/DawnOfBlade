using DawnOfBlade.Core;
using Godot;

namespace DawnOfBlade.Interaction;

public partial class PrototypeNpc : Interactable
{
    [Export] public string SpeakerName { get; set; } = "Ari";

    public override void Interact(Node interactor)
    {
        var gameManager = GetTree().CurrentScene as GameManager;
        gameManager?.ShowNpcDialogue(SpeakerName);
    }
}
