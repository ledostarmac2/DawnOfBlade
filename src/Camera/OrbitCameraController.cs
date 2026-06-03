using Godot;

namespace DawnOfBlade.Camera;

public partial class OrbitCameraController : Node3D
{
    [Export] public NodePath FollowTargetPath { get; set; } = new("");
    [Export] public float FollowSmoothing { get; set; } = 12.0f;
    [Export] public float RotationSpeed { get; set; } = 0.01f;
    [Export] public float ZoomSpeed { get; set; } = 0.65f;
    [Export] public float KeyboardZoomSpeed { get; set; } = 5.0f;
    [Export] public float KeyboardPanSpeed { get; set; } = 13.0f;
    [Export] public float MaxPanDistance { get; set; } = 28.0f;
    [Export] public float MinZoom { get; set; } = 2.5f;
    [Export] public float MaxZoom { get; set; } = 16.0f;
    [Export] public float CameraHeight { get; set; } = 4.0f;

    private Node3D? _followTarget;
    private Camera3D? _camera;
    private float _zoom = 6.5f;
    private Vector3 _panOffset = Vector3.Zero;

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
        if (!IsInsideTree())
        {
            return;
        }

        var deltaSeconds = (float)delta;
        HandleKeyboardCamera(deltaSeconds);

        var anchor = _followTarget is not null && _followTarget.IsInsideTree()
            ? _followTarget.GlobalPosition
            : GlobalPosition;
        var desiredPosition = anchor + _panOffset;
        var weight = 1.0f - Mathf.Exp(-FollowSmoothing * deltaSeconds);
        GlobalPosition = GlobalPosition.Lerp(desiredPosition, weight);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (_camera is null || !_camera.IsInsideTree() || !IsInsideTree())
        {
            return;
        }

        _camera.Position = new Vector3(0, CameraHeight, _zoom);
        _camera.LookAt(GlobalPosition, Vector3.Up);
    }

    private void HandleKeyboardCamera(float delta)
    {
        if (IsTextInputFocused())
        {
            return;
        }

        var input = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up))
        {
            input.Y -= 1.0f;
        }

        if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down))
        {
            input.Y += 1.0f;
        }

        if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left))
        {
            input.X -= 1.0f;
        }

        if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right))
        {
            input.X += 1.0f;
        }

        if (input.LengthSquared() > 0.0f)
        {
            input = input.Normalized();
            var forward = -GlobalTransform.Basis.Z;
            forward.Y = 0.0f;
            forward = forward.LengthSquared() > 0.0f ? forward.Normalized() : Vector3.Forward;

            var right = GlobalTransform.Basis.X;
            right.Y = 0.0f;
            right = right.LengthSquared() > 0.0f ? right.Normalized() : Vector3.Right;

            _panOffset += (right * input.X + forward * -input.Y) * KeyboardPanSpeed * delta;
            if (_panOffset.Length() > MaxPanDistance)
            {
                _panOffset = _panOffset.Normalized() * MaxPanDistance;
            }
        }

        if (Input.IsPhysicalKeyPressed(Key.Q))
        {
            _zoom = Mathf.Clamp(_zoom - KeyboardZoomSpeed * delta, MinZoom, MaxZoom);
        }

        if (Input.IsPhysicalKeyPressed(Key.E))
        {
            _zoom = Mathf.Clamp(_zoom + KeyboardZoomSpeed * delta, MinZoom, MaxZoom);
        }

        if (Input.IsPhysicalKeyPressed(Key.Home))
        {
            _panOffset = Vector3.Zero;
        }
    }

    private bool IsTextInputFocused() =>
        GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit;
}
