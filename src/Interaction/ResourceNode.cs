using DawnOfBlade.Core;
using Godot;

namespace DawnOfBlade.Interaction;

public partial class ResourceNode : Interactable
{
    public override void Interact(Node interactor)
    {
        var gameManager = GetTree().CurrentScene as GameManager;
        gameManager?.GatherSunShard();
    }
}
