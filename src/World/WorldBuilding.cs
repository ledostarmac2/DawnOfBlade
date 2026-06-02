using Godot;

namespace DawnOfBlade.World;

/// <summary>A generated enterable landmark whose roof fades while the player is within its footprint.</summary>
public partial class WorldBuilding : Node3D
{
    private readonly Vector2 _footprint;
    private readonly float _wallHeight;
    private readonly StandardMaterial3D _roofMaterial;
    private Node3D? _player;

    public WorldBuilding(string displayName, Vector2 footprint, float wallHeight, Color wallColor, Color roofColor)
    {
        Name = displayName;
        _footprint = footprint;
        _wallHeight = wallHeight;
        _roofMaterial = new StandardMaterial3D
        {
            AlbedoColor = roofColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        Build(wallColor);
    }

    public void Follow(Node3D player) => _player = player;

    public override void _Process(double delta)
    {
        if (_player is null)
        {
            return;
        }

        var local = ToLocal(_player.GlobalPosition);
        var inside = Mathf.Abs(local.X) < _footprint.X * 0.5f - 0.7f &&
                     Mathf.Abs(local.Z) < _footprint.Y * 0.5f - 0.7f;
        var color = _roofMaterial.AlbedoColor;
        var targetAlpha = inside ? 0.12f : 1.0f;
        color.A = Mathf.Lerp(color.A, targetAlpha, 0.16f);
        _roofMaterial.AlbedoColor = color;
    }

    private void Build(Color wallColor)
    {
        const float thickness = 0.55f;
        var doorWidth = Mathf.Min(3.2f, _footprint.X * 0.22f);
        AddWall(new Vector3(-(_footprint.X + doorWidth) * 0.25f, _wallHeight * 0.5f, _footprint.Y * 0.5f), new Vector3((_footprint.X - doorWidth) * 0.5f, _wallHeight, thickness), wallColor);
        AddWall(new Vector3((_footprint.X + doorWidth) * 0.25f, _wallHeight * 0.5f, _footprint.Y * 0.5f), new Vector3((_footprint.X - doorWidth) * 0.5f, _wallHeight, thickness), wallColor);
        AddWall(new Vector3(0, _wallHeight * 0.5f, -_footprint.Y * 0.5f), new Vector3(_footprint.X, _wallHeight, thickness), wallColor);
        AddWall(new Vector3(-_footprint.X * 0.5f, _wallHeight * 0.5f, 0), new Vector3(thickness, _wallHeight, _footprint.Y), wallColor);
        AddWall(new Vector3(_footprint.X * 0.5f, _wallHeight * 0.5f, 0), new Vector3(thickness, _wallHeight, _footprint.Y), wallColor);

        var roof = new MeshInstance3D
        {
            Name = "Roof",
            Mesh = new PrismMesh { Size = new Vector3(_footprint.X + 1.2f, 3.2f, _footprint.Y + 1.2f) },
            Position = new Vector3(0, _wallHeight + 1.4f, 0),
            MaterialOverride = _roofMaterial,
        };
        AddChild(roof);
    }

    private void AddWall(Vector3 position, Vector3 size, Color color)
    {
        var body = new StaticBody3D { Position = position };
        body.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
        });
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }
}
