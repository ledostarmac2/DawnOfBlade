using Godot;
using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Characters;
using DawnOfBlade.Interaction;
using DawnOfBlade.Movement;
using DawnOfBlade.World;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.RiverValley;

namespace DawnOfBlade.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float RunSpeed { get; set; } = 8.0f;
    [Export] public float InteractionDistance { get; set; } = 2.15f;
    [Export] public NodePath MoveTargetMarkerPath { get; set; } = new("");
    [Export] public float RaycastDistance { get; set; } = 1000.0f;
    [Export] public bool ConstrainToRegionTiles { get; set; } = true;

    public float RunEnergy { get; private set; } = 100.0f;
    public bool IsRunning { get; private set; }
    public bool IsMoving => _movement.TargetPosition is not null;

    private ClickToMoveController _movement = new();
    private readonly RiverValleyRegion _region = new();
    private GridPathfinder? _pathfinder;
    private Node3D? _moveTargetMarker;
    private Interactable? _pendingInteraction;
    private PopupMenu? _contextMenu;
    private Interactable? _contextInteractable;
    private Vector3 _contextPosition;

    public override void _Ready()
    {
        _movement.MoveSpeed = MoveSpeed;
        _pathfinder = new GridPathfinder(_region.IsWalkable);
        _moveTargetMarker = MoveTargetMarkerPath.IsEmpty ? GetParent()?.GetNodeOrNull<Node3D>("MoveTarget") : GetNodeOrNull<Node3D>(MoveTargetMarkerPath);

        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.Visible = false;
        }

        _contextMenu = new PopupMenu();
        _contextMenu.IdPressed += OnContextAction;
        AddChild(_contextMenu);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TrySetMoveTargetFromMouse();
        }

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            ShowContextMenu();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var previousPosition = GlobalPosition;
        TryCompletePendingInteraction();
        _movement.MoveSpeed = IsRunning ? RunSpeed : MoveSpeed;
        Velocity = _movement.GetVelocity(GlobalPosition);
        UpdateFacingAndAnimation();
        MoveAndSlide();
        EnforceRegionCollision(previousPosition);
        TryCompletePendingInteraction();
    }

    public void FaceTowards(Vector3 target)
    {
        var direction = target - GlobalPosition;
        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.001f)
        {
            Rotation = new Vector3(Rotation.X, Mathf.Atan2(-direction.X, -direction.Z), Rotation.Z);
        }
    }

    public void PlayAttack(string? weaponItemId)
    {
        GetNodeOrNull<HumanoidVisual>("Humanoid")?.PlayAttack(weaponItemId);
    }

    public void ToggleRun()
    {
        if (!IsRunning && RunEnergy <= 0.0f)
        {
            return;
        }

        IsRunning = !IsRunning;
    }

    public void TeleportTo(Vector3 position)
    {
        _movement.ClearTarget();
        _pendingInteraction = null;
        Velocity = Vector3.Zero;
        GlobalPosition = position;

        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.Visible = false;
        }
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

    private void ShowContextMenu()
    {
        if (_contextMenu is null || !TryRaycastMouse(out var hit))
        {
            return;
        }

        _contextMenu.Clear();
        _contextInteractable = FindInteractable(hit);
        _contextPosition = hit.TryGetValue("position", out var positionVariant) ? positionVariant.AsVector3() : GlobalPosition;
        if (_contextInteractable is not null)
        {
            _contextMenu.AddItem($"Interact: {_contextInteractable.DisplayName}", 1);
            _contextMenu.AddItem($"Examine: {_contextInteractable.DisplayName}", 2);
        }

        _contextMenu.AddItem("Walk here", 3);
        var mouse = GetViewport().GetMousePosition();
        _contextMenu.Position = new Vector2I(Mathf.RoundToInt(mouse.X), Mathf.RoundToInt(mouse.Y));
        _contextMenu.Popup();
    }

    private void OnContextAction(long id)
    {
        switch (id)
        {
            case 1 when _contextInteractable is not null:
                _pendingInteraction = _contextInteractable;
                if (SetMoveTarget(_contextInteractable.GlobalPosition, allowNearestWalkable: true))
                {
                    TryCompletePendingInteraction();
                }
                else
                {
                    _pendingInteraction = null;
                }
                break;
            case 2 when _contextInteractable is not null:
                GD.Print($"Examine: {_contextInteractable.DisplayName}");
                break;
            case 3:
                _pendingInteraction = null;
                SetMoveTarget(_contextPosition);
                break;
        }
    }

    private bool TryRaycastMouse(out Godot.Collections.Dictionary hit)
    {
        hit = new Godot.Collections.Dictionary();
        var camera = GetViewport().GetCamera3D();
        if (camera is null)
        {
            return false;
        }

        var mousePosition = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePosition);
        var rayEnd = rayOrigin + camera.ProjectRayNormal(mousePosition) * RaycastDistance;
        hit = GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd));
        return hit.Count > 0;
    }

    private bool TryQueueInteraction(Godot.Collections.Dictionary hit)
    {
        if (!hit.TryGetValue("collider", out var colliderVariant))
        {
            return false;
        }

        var interactable = FindInteractable(hit);
        if (interactable is null || !interactable.CanInteract(this))
        {
            return false;
        }

        _pendingInteraction = interactable;
        if (!SetMoveTarget(interactable.GlobalPosition, allowNearestWalkable: true))
        {
            _pendingInteraction = null;
            return false;
        }

        TryCompletePendingInteraction();
        return true;
    }

    private static Interactable? FindInteractable(Godot.Collections.Dictionary hit)
    {
        if (!hit.TryGetValue("collider", out var colliderVariant))
        {
            return null;
        }

        var collider = colliderVariant.AsGodotObject() as Node;
        return collider as Interactable ?? collider?.GetParentOrNull<Interactable>();
    }

    private bool SetMoveTarget(Vector3 targetPosition, bool allowNearestWalkable = false)
    {
        if (ConstrainToRegionTiles && TryBuildTilePath(targetPosition, allowNearestWalkable, out var destination, out var waypoints))
        {
            _movement.SetPath(waypoints);
            UpdateMoveMarker(destination);
            return true;
        }

        if (ConstrainToRegionTiles)
        {
            return false;
        }

        _movement.SetTargetPosition(targetPosition);
        UpdateMoveMarker(targetPosition);
        return true;
    }

    private void UpdateMoveMarker(Vector3 targetPosition)
    {
        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.GlobalPosition = targetPosition + Vector3.Up * 0.03f;
            _moveTargetMarker.Visible = true;
        }
    }

    private bool TryBuildTilePath(
        Vector3 requestedPosition,
        bool allowNearestWalkable,
        out Vector3 destination,
        out IReadOnlyList<Vector3> waypoints)
    {
        destination = requestedPosition;
        waypoints = System.Array.Empty<Vector3>();

        if (_pathfinder is null)
        {
            return false;
        }

        var start = WorldToTile(GlobalPosition);
        var target = WorldToTile(requestedPosition);
        if (!_region.IsWalkable(target))
        {
            if (!allowNearestWalkable || !TryFindNearestWalkable(target, start, out target))
            {
                return false;
            }
        }

        destination = TileToWorld(target);
        if (start == target)
        {
            waypoints = new[] { destination };
            return true;
        }

        var path = _pathfinder.FindPath(start, target);
        if (path.Count == 0)
        {
            return false;
        }

        waypoints = path.Select(TileToWorld).ToArray();
        return true;
    }

    private bool TryFindNearestWalkable(GridCoordinate center, GridCoordinate preferredFrom, out GridCoordinate nearest)
    {
        nearest = center;
        var bestScore = int.MaxValue;
        for (var radius = 1; radius <= 4; radius++)
        {
            for (var z = center.Z - radius; z <= center.Z + radius; z++)
            {
                for (var x = center.X - radius; x <= center.X + radius; x++)
                {
                    if (System.Math.Max(System.Math.Abs(x - center.X), System.Math.Abs(z - center.Z)) != radius)
                    {
                        continue;
                    }

                    var candidate = new GridCoordinate(x, z);
                    if (!_region.IsWalkable(candidate))
                    {
                        continue;
                    }

                    var score = System.Math.Abs(candidate.X - preferredFrom.X) + System.Math.Abs(candidate.Z - preferredFrom.Z);
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    nearest = candidate;
                    bestScore = score;
                }
            }

            if (bestScore < int.MaxValue)
            {
                return true;
            }
        }

        return false;
    }

    private void EnforceRegionCollision(Vector3 previousPosition)
    {
        if (!ConstrainToRegionTiles || IsWorldPositionWalkable(GlobalPosition))
        {
            return;
        }

        GlobalPosition = previousPosition;
        Velocity = Vector3.Zero;
        _movement.ClearTarget();
        if (_moveTargetMarker is not null)
        {
            _moveTargetMarker.Visible = false;
        }
    }

    private bool IsWorldPositionWalkable(Vector3 position) =>
        _region.IsWalkable(WorldToTile(position));

    private GridCoordinate WorldToTile(Vector3 position)
    {
        var tileSize = RiverValleyRegion.TileSizeMeters * OpenWorldBuilder.VisualWorldScale;
        return new GridCoordinate(
            _region.RespawnTile.X + Mathf.RoundToInt(position.X / tileSize),
            _region.RespawnTile.Z + Mathf.RoundToInt(position.Z / tileSize));
    }

    private Vector3 TileToWorld(GridCoordinate tile)
    {
        var world = _region.TileToWorld(tile, GlobalPosition.Y);
        return new Vector3(
            world.X * OpenWorldBuilder.VisualWorldScale,
            GlobalPosition.Y,
            world.Z * OpenWorldBuilder.VisualWorldScale);
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

    private void UpdateFacingAndAnimation()
    {
        var horizontalVelocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
        if (horizontalVelocity.LengthSquared() > 0.001f)
        {
            FaceTowards(GlobalPosition + horizontalVelocity);
        }

        GetNodeOrNull<HumanoidVisual>("Humanoid")?.SetLocomotion(
            horizontalVelocity.LengthSquared() > 0.001f,
            horizontalVelocity.Length());
    }
}
