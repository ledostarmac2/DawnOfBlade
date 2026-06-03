using DawnOfBlade.World;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.RiverValley;
using Godot;

namespace DawnOfBlade.UI;

/// <summary>Compact circular overview of the starting region around the current player position.</summary>
public partial class MiniMapControl : Control
{
    [Export] public float MapRadiusMeters { get; set; } = 130.0f;

    private readonly RiverValleyRegion _region = new();
    private Node3D? _trackedPlayer;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(176, 176);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public void SetTrackedPlayer(Node3D? player)
    {
        _trackedPlayer = player;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var center = Size * 0.5f;
        var radius = Mathf.Min(Size.X, Size.Y) * 0.5f - 7.0f;
        DrawCircle(center, radius + 4.0f, new Color("#2a2117"));
        DrawCircle(center, radius, new Color("#173524"));

        DrawRoad(center, radius, new GridCoordinate(35, 0), new GridCoordinate(35, 127), "#8f7955", 2.0f);
        DrawRoad(center, radius, new GridCoordinate(0, 35), new GridCoordinate(127, 35), "#8f7955", 2.0f);
        DrawRoad(center, radius, new GridCoordinate(20, 20), new GridCoordinate(50, 20), "#71675a", 2.0f);
        DrawRoad(center, radius, new GridCoordinate(20, 50), new GridCoordinate(50, 50), "#71675a", 2.0f);
        DrawRoad(center, radius, new GridCoordinate(57, 0), new GridCoordinate(57, 127), "#2f85a5", 5.0f);

        DrawDistrict(center, radius, _region.CastleBounds, "#7f8580");
        DrawDistrict(center, radius, _region.PastureBounds, "#6b8b48");
        DrawDistrict(center, radius, _region.WoodlandsBounds, "#2f6a38");
        DrawDistrict(center, radius, _region.MineBounds, "#6f695e");

        DrawMarker(center, radius, _region.RespawnTile, "#f2d36b", 5.0f);
        DrawMarker(center, radius, new GridCoordinate(35, 10), "#d39a45", 4.0f);
        DrawMarker(center, radius, new GridCoordinate(57, 35), "#cfb17a", 4.0f);

        foreach (var anchor in _region.AnchorsOfType(RegionAnchorType.Resource))
        {
            DrawMarker(center, radius, anchor.Coordinate, anchor.InteractionType == "woodcutting" ? "#57b65d" : "#c18c64", 2.0f);
        }

        if (_trackedPlayer is not null && _trackedPlayer.IsInsideTree())
        {
            var player = WorldToMap(_trackedPlayer.GlobalPosition, center, radius);
            DrawCircle(player, 5.2f, new Color("#0b1110"));
            DrawCircle(player, 3.7f, new Color("#ffffff"));

            var forward = -_trackedPlayer.GlobalTransform.Basis.Z;
            var heading = new Vector2(forward.X, forward.Z);
            if (heading.LengthSquared() > 0.0f)
            {
                DrawLine(player, player + heading.Normalized() * 9.0f, new Color("#ffffff"), 2.0f);
            }
        }

        DrawArc(center, radius + 2.0f, 0.0f, Mathf.Pi * 2.0f, 96, new Color("#c2a260"), 3.0f);
    }

    private void DrawDistrict(Vector2 center, float radius, GridBounds bounds, string color)
    {
        DrawMarker(center, radius, bounds.Minimum, color, 2.0f);
        DrawMarker(center, radius, bounds.Maximum, color, 2.0f);
        var midpoint = new GridCoordinate((bounds.Minimum.X + bounds.Maximum.X) / 2, (bounds.Minimum.Z + bounds.Maximum.Z) / 2);
        DrawMarker(center, radius, midpoint, color, 4.0f);
    }

    private void DrawRoad(Vector2 center, float radius, GridCoordinate from, GridCoordinate to, string color, float width)
    {
        var a = TileToMap(from, center, radius);
        var b = TileToMap(to, center, radius);
        DrawLine(ClampToCircle(center, radius, a), ClampToCircle(center, radius, b), new Color(color), width);
    }

    private void DrawMarker(Vector2 center, float radius, GridCoordinate tile, string color, float markerRadius)
    {
        var position = TileToMap(tile, center, radius);
        if (position.DistanceSquaredTo(center) <= radius * radius)
        {
            DrawCircle(position, markerRadius, new Color(color));
        }
    }

    private Vector2 TileToMap(GridCoordinate tile, Vector2 center, float radius)
    {
        var world = _region.TileToWorld(tile);
        world.X *= OpenWorldBuilder.VisualWorldScale;
        world.Z *= OpenWorldBuilder.VisualWorldScale;
        return WorldToMap(world, center, radius);
    }

    private Vector2 WorldToMap(Vector3 world, Vector2 center, float radius)
    {
        var offset = new Vector2(world.X, world.Z) / MapRadiusMeters * radius;
        return ClampToCircle(center, radius, center + offset);
    }

    private static Vector2 ClampToCircle(Vector2 center, float radius, Vector2 position)
    {
        var offset = position - center;
        return offset.Length() > radius ? center + offset.Normalized() * radius : position;
    }
}
