using Godot;

namespace DawnOfBlade.Interaction;

public abstract partial class Interactable : StaticBody3D
{
    [Export] public string DisplayName { get; set; } = "Interactable";

    public virtual bool CanInteract(Node interactor)
    {
        return true;
    }

    public virtual void Interact(Node interactor)
    {
        // TODO: Emit an interaction event once the interaction service exists.
        GD.Print($"{interactor.Name} interacted with {DisplayName}.");
    }
}
