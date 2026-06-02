using DawnOfBlade.Core;
using Godot;

namespace DawnOfBlade.Interaction;

/// <summary>Clickable local-world item drop with tick-based expiration.</summary>
public partial class GroundLootNode : Interactable
{
    [Export] public string ItemId { get; set; } = "coins";
    [Export] public int Quantity { get; set; } = 1;
    [Export] public long ExpiresAtTick { get; set; } = 200;

    public override void _Ready()
    {
        AddToGroup("ground_loot");
    }

    public void AdvanceTick(long currentTick)
    {
        if (currentTick >= ExpiresAtTick)
        {
            QueueFree();
        }
    }

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.PickUpGroundLoot(this);
    }
}
