using Godot;
using DawnOfBlade.Movement;

namespace DawnOfBlade.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;

    private ClickToMoveController _movement = new();

    public override void _Ready()
    {
        _movement.MoveSpeed = MoveSpeed;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            // TODO: Replace this with a camera raycast into the navigation world.
            _movement.SetTargetPosition(GlobalPosition + -GlobalTransform.Basis.Z * 3.0f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = _movement.GetVelocity(GlobalPosition);
        MoveAndSlide();
    }
}

