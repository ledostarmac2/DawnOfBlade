using DawnOfBlade.Core;
using Godot;

namespace DawnOfBlade.Interaction;

public partial class ResourceNode : Interactable
{
    [Export] public string ItemId { get; set; } = "sunleaf";
    [Export] public string SkillId { get; set; } = "foraging";
    [Export] public int Experience { get; set; } = 15;

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.GatherResource(this);
    }
}
