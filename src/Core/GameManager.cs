using Godot;

namespace DawnOfBlade.Core;

public partial class GameManager : Node
{
    public bool IsInitialized { get; private set; }

    public override void _Ready()
    {
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        // TODO: Register save, data loading, UI, and gameplay services as they are implemented.
        IsInitialized = true;
        GD.Print("Dawn of Blade core systems initialized.");
    }
}
