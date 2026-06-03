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
        if (_player is null || !IsInsideTree() || !_player.IsInsideTree())
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

        AddMasonryCourses();
        AddTrim();
        AddDoor(doorWidth);
        AddWindows();
        AddChimney();
        AddRoofRidge();
    }

    private void AddWall(Vector3 position, Vector3 size, Color color)
    {
        var body = new StaticBody3D { Position = position };
        body.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = WallMaterial(color),
        });
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }

    private void AddTrim()
    {
        var y = _wallHeight + 0.08f;
        var color = new Color("#3b2b20");
        AddDecor(new Vector3(0, y, _footprint.Y * 0.5f + 0.34f), new Vector3(_footprint.X + 0.8f, 0.18f, 0.18f), color);
        AddDecor(new Vector3(0, y, -_footprint.Y * 0.5f - 0.34f), new Vector3(_footprint.X + 0.8f, 0.18f, 0.18f), color);
        AddDecor(new Vector3(_footprint.X * 0.5f + 0.34f, y, 0), new Vector3(0.18f, 0.18f, _footprint.Y + 0.8f), color);
        AddDecor(new Vector3(-_footprint.X * 0.5f - 0.34f, y, 0), new Vector3(0.18f, 0.18f, _footprint.Y + 0.8f), color);
    }

    private void AddMasonryCourses()
    {
        var lineColor = new Color("#5c5145");
        var frontZ = _footprint.Y * 0.5f + 0.31f;
        var backZ = -_footprint.Y * 0.5f - 0.31f;
        var sideX = _footprint.X * 0.5f + 0.31f;
        for (var y = 1.05f; y < _wallHeight - 0.4f; y += 0.85f)
        {
            AddDecor(new Vector3(0, y, frontZ), new Vector3(_footprint.X * 0.82f, 0.035f, 0.04f), lineColor);
            AddDecor(new Vector3(0, y, backZ), new Vector3(_footprint.X * 0.82f, 0.035f, 0.04f), lineColor);
            AddDecor(new Vector3(sideX, y, 0), new Vector3(0.04f, 0.035f, _footprint.Y * 0.82f), lineColor);
            AddDecor(new Vector3(-sideX, y, 0), new Vector3(0.04f, 0.035f, _footprint.Y * 0.82f), lineColor);
        }
    }

    private void AddDoor(float doorWidth)
    {
        AddDecor(
            new Vector3(0, 1.18f, _footprint.Y * 0.5f + 0.35f),
            new Vector3(doorWidth * 0.62f, 2.35f, 0.12f),
            new Color("#4a2f20"));
        AddDecor(
            new Vector3(doorWidth * 0.22f, 1.24f, _footprint.Y * 0.5f + 0.43f),
            new Vector3(0.08f, 0.08f, 0.08f),
            new Color("#d1a445"));
    }

    private void AddWindows()
    {
        var windowColor = new Color("#b9d4d8");
        var frameColor = new Color("#3a2a1e");
        var frontZ = _footprint.Y * 0.5f + 0.36f;
        var backZ = -_footprint.Y * 0.5f - 0.36f;
        var sideX = _footprint.X * 0.5f + 0.36f;
        var windowY = Mathf.Min(_wallHeight * 0.58f, 3.4f);

        foreach (var x in WindowOffsets(_footprint.X))
        {
            AddDecor(new Vector3(x, windowY, frontZ), new Vector3(0.72f, 0.62f, 0.08f), windowColor);
            AddDecor(new Vector3(x, windowY, frontZ + 0.05f), new Vector3(0.84f, 0.08f, 0.09f), frameColor);
            AddDecor(new Vector3(x, windowY, backZ), new Vector3(0.72f, 0.62f, 0.08f), windowColor);
        }

        foreach (var z in WindowOffsets(_footprint.Y))
        {
            AddDecor(new Vector3(sideX, windowY, z), new Vector3(0.08f, 0.62f, 0.72f), windowColor);
            AddDecor(new Vector3(-sideX, windowY, z), new Vector3(0.08f, 0.62f, 0.72f), windowColor);
        }
    }

    private void AddChimney()
    {
        var x = _footprint.X * 0.22f;
        var z = -_footprint.Y * 0.18f;
        AddDecor(
            new Vector3(x, _wallHeight + 2.15f, z),
            new Vector3(0.8f, 1.9f, 0.8f),
            new Color("#5d5248"));
    }

    private void AddRoofRidge()
    {
        AddDecor(
            new Vector3(0, _wallHeight + 3.1f, 0),
            new Vector3(_footprint.X + 1.7f, 0.18f, 0.24f),
            new Color("#2f2421"));
    }

    private void AddDecor(Vector3 position, Vector3 size, Color color)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = position,
            MaterialOverride = WallMaterial(color),
        });
    }

    private static float[] WindowOffsets(float length)
    {
        if (length < 18.0f)
        {
            return new[] { 0.0f };
        }

        return new[] { -length * 0.25f, length * 0.25f };
    }

    private static StandardMaterial3D WallMaterial(Color color) => new()
    {
        AlbedoColor = color,
        Roughness = 0.86f,
        Metallic = 0.0f,
    };
}
