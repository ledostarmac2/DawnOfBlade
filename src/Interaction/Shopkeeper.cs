using DawnOfBlade.Core;
using Godot;

namespace DawnOfBlade.Interaction;

/// <summary>An interactable that opens a shop window through the game manager.</summary>
public partial class Shopkeeper : Interactable
{
    [Export] public string ShopId { get; set; } = "village_general";

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.OpenShop(ShopId);
    }
}
