using Godot;
using DawnOfBlade.Interaction;
using DawnOfBlade.Movement;

namespace DawnOfBlade.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float RunSpeed { get; set; } = 8.0f;
    [Export] public float InteractionDistance { get; set; } = 1.6f;
    [Export] public NodePath MoveTargetMarkerPath { get; set; } = new("");
    [Export] public float RaycastDistance { get; set; } = 1000.0f;

    public float RunEnergy { get; private set; } = 100.0f;
    public bool IsRunning { get; private set; }
    public bool IsMoving => _movement.TargetPosition is not null;

    private ClickToMoveController _movement = new();
    private Node3D? _moveTargetMarker;
    private Interactable? _pendingInteraction;

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
        TryCompletePendingInteraction();
        _movement.MoveSpeed = IsRunning ? RunSpeed : MoveSpeed;
        Velocity = _movement.GetVelocity(GlobalPosition);
        MoveAndSlide();
        TryCompletePendingInteraction();
    }

    public void ToggleRun()
    {
        if (!IsRunning && RunEnergy <= 0.0f)
        {
            return;
        }

        IsRunning = !IsRunning;
    }

    /// <summary>
    /// Advances the local sandbox stamina model. The authoritative simulation can replace this
    /// call later while preserving the HUD-facing values.
    /// </summary>
    public void ApplyLocalTick()
    {
        if (IsRunning && IsMoving)
        {
            RunEnergy = Mathf.Max(0.0f, RunEnergy - 4.0f);
            if (RunEnergy <= 0.0f)
            {
                IsRunning = false;
            }

            return;
        }

        RunEnergy = Mathf.Min(100.0f, RunEnergy + 2.0f);
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

        if (TryQueueInteraction(hit))
        {
            return;
        }

        if (!hit.TryGetValue("position", out var positionVariant))
        {
            return;
        }

        var targetPosition = positionVariant.AsVector3();
        _pendingInteraction = null;
        SetMoveTarget(targetPosition);
    }

    private bool TryQueueInteraction(Godot.Collections.Dictionary hit)
    {
        if (!hit.TryGetValue("collider", out var colliderVariant))
        {
            return false;
        }

        var collider = colliderVariant.AsGodotObject() as Node;
        var interactable = collider as Interactable ?? collider?.GetParentOrNull<Interactable>();
        if (interactable is null || !interactable.CanInteract(this))
        {
            return false;
        }

        _pendingInteraction = interactable;
        SetMoveTarget(interactable.GlobalPosition);
        TryCompletePendingInteraction();
        return true;
    }

    private void SetMoveTarget(Vector3 targetPosition)
    {
        _movement.SetTargetPosition(targetPosition);
        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.GlobalPosition = targetPosition + Vector3.Up * 0.03f;
            _moveTargetMarker.Visible = true;
        }
    }

    private void TryCompletePendingInteraction()
    {
        var interactable = _pendingInteraction;
        if (interactable is null || !GodotObject.IsInstanceValid(interactable))
        {
            _pendingInteraction = null;
            return;
        }

        var offset = interactable.GlobalPosition - GlobalPosition;
        offset.Y = 0.0f;
        if (offset.Length() > InteractionDistance)
        {
            return;
        }

        _movement.ClearTarget();
        _pendingInteraction = null;
        if (interactable.CanInteract(this))
        {
            interactable.Interact(this);
        }

        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.Visible = false;
        }
    }
}
