using DawnOfBlade.Core;
using DawnOfBlade.World;
using Godot;

namespace DawnOfBlade.Interaction;

public partial class ResourceNode : Interactable
{
    [Export] public string ItemId { get; set; } = "sunleaf";
    [Export] public string SkillId { get; set; } = "foraging";
    [Export] public int VisualVariant { get; set; }
    [Export] public int Experience { get; set; } = 15;
    [Export] public int RespawnTicks { get; set; } = 20;

    public bool IsDepleted => _respawn.IsDepleted;
    public long RespawnsAtTick => _respawn.RespawnsAtTick;

    private readonly ResourceRespawnState _respawn = new();

    public override void _Ready()
    {
        AddToGroup("resource_nodes");
        if (GetNodeOrNull<Node3D>("Mesh") is { } placeholder)
        {
            placeholder.Visible = false;
        }

        AddChild(GeneratedAssetFactory.CreateResource(ItemId, VisualVariant));
    }

    public override bool CanInteract(Node interactor) => !IsDepleted;

    public void Deplete(long currentTick)
    {
        _respawn.Deplete(currentTick, RespawnTicks);
        Visible = false;
        SetCollisionLayerValue(1, false);
    }

    public void AdvanceTick(long currentTick)
    {
        if (!_respawn.AdvanceTick(currentTick))
        {
            return;
        }

        Visible = true;
        SetCollisionLayerValue(1, true);
    }

    public override void Interact(Node interactor)
    {
        (GetTree().CurrentScene as GameManager)?.GatherResource(this);
    }
}
