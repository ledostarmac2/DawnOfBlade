using Godot;

namespace DawnOfBlade.Camera;

public partial class OrbitCameraController : Node3D
{
    [Export] public NodePath FollowTargetPath { get; set; } = new("");
    [Export] public float FollowSmoothing { get; set; } = 12.0f;
    [Export] public float RotationSpeed { get; set; } = 0.01f;
    [Export] public float ZoomSpeed { get; set; } = 0.5f;
    [Export] public float MinZoom { get; set; } = 4.0f;
    [Export] public float MaxZoom { get; set; } = 12.0f;

    private Node3D? _followTarget;
    private Camera3D? _camera;
    private float _zoom = 7.0f;

    public override void _Ready()
    {
        _followTarget = FollowTargetPath.IsEmpty ? GetParent()?.GetNodeOrNull<Node3D>("Player") : GetNodeOrNull<Node3D>(FollowTargetPath);
        _camera = GetNodeOrNull<Camera3D>("Camera3D");
        ApplyZoom();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Right))
        {
            RotateY(-motion.Relative.X * RotationSpeed);
        }

        if (@event is InputEventMouseButton { Pressed: true } button)
        {
            if (button.ButtonIndex == MouseButton.WheelUp)
            {
                _zoom = Mathf.Clamp(_zoom - ZoomSpeed, MinZoom, MaxZoom);
            }
            else if (button.ButtonIndex == MouseButton.WheelDown)
            {
                _zoom = Mathf.Clamp(_zoom + ZoomSpeed, MinZoom, MaxZoom);
            }

            ApplyZoom();
        }
    }

    public override void _Process(double delta)
    {
        if (_followTarget is null)
        {
            return;
        }

        var weight = 1.0f - Mathf.Exp(-FollowSmoothing * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(_followTarget.GlobalPosition, weight);
    }

    private void ApplyZoom()
    {
        if (_camera is null)
        {
            return;
        }

        _camera.Position = new Vector3(0, 4, _zoom);
        _camera.LookAt(GlobalPosition, Vector3.Up);
    }
}
