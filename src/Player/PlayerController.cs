using Godot;
using DawnOfBlade.Movement;

namespace DawnOfBlade.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public NodePath MoveTargetMarkerPath { get; set; } = new("");
    [Export] public float RaycastDistance { get; set; } = 1000.0f;

    private ClickToMoveController _movement = new();
    private Node3D? _moveTargetMarker;

    public override void _Ready()
    {
        _movement.MoveSpeed = MoveSpeed;
        _moveTargetMarker = MoveTargetMarkerPath.IsEmpty ? GetParent()?.GetNodeOrNull<Node3D>("MoveTarget") : GetNodeOrNull<Node3D>(MoveTargetMarkerPath);

        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.Visible = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TrySetMoveTargetFromMouse();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = _movement.GetVelocity(GlobalPosition);
        MoveAndSlide();
    }

    private void TrySetMoveTargetFromMouse()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera is null)
        {
            return;
        }

        var mousePosition = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePosition);
        var rayEnd = rayOrigin + camera.ProjectRayNormal(mousePosition) * RaycastDistance;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (!hit.TryGetValue("position", out var positionVariant))
        {
            return;
        }

        var targetPosition = positionVariant.AsVector3();
        _movement.SetTargetPosition(targetPosition);

        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.GlobalPosition = targetPosition + Vector3.Up * 0.03f;
            _moveTargetMarker.Visible = true;
        }
    }
}
