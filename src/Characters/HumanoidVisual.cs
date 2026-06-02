using Godot;

namespace DawnOfBlade.Characters;

/// <summary>
/// Procedural retro low-poly humanoid (Parts 27-30). Built from faceted, tapered primitives — not
/// boxes — with a strict-ish poly budget, flat/no-PBR materials and nearest-neighbour texture
/// filtering. Limbs are segmented (upper/lower) around rigid elbow/knee pivots, weapons and shields
/// socket to explicit hand markers, and worn armour swaps the affected body sub-mesh rather than
/// layering. Hand-painted textures and uniquely modelled per-tier meshes are intentionally left to
/// the art pipeline; this is the engine-correct scaffold they drop into.
/// </summary>
public partial class HumanoidVisual : Node3D
{
    [Export] public string SkinTone { get; set; } = "#e0b48c";
    [Export] public string HairColor { get; set; } = "#3a2a1a";
    [Export] public string ShirtColor { get; set; } = "#6a5acd";
    [Export] public string LegColor { get; set; } = "#3b3b46";
    [Export] public string BodyType { get; set; } = "slim";

    private const float AttackDuration = 0.52f;
    private const int Facets = 6; // low radial segment count -> visible flat facets

    private float _animationTime;
    private float _movementBlend;
    private float _attackRemaining;
    private WeaponAnimationType _weaponAnimation = WeaponAnimationType.Unarmed;

    // Animated joints / sockets, resolved once per Apply() so the pose loop never name-searches.
    private Node3D? _torso;
    private Node3D? _head;
    private Node3D? _leftShoulder;
    private Node3D? _rightShoulder;
    private Node3D? _leftElbow;
    private Node3D? _rightElbow;
    private Node3D? _leftHip;
    private Node3D? _rightHip;
    private Node3D? _leftKnee;
    private Node3D? _rightKnee;
    private Node3D? _rightHandSocket;
    private Node3D? _leftHandSocket;
    private Node3D? _weapon;
    private Node3D? _shield;
    private MeshInstance3D? _torsoMesh;
    private MeshInstance3D? _legArmorAnchorL;
    private MeshInstance3D? _legArmorAnchorR;

    private Appearance _appearance = new();

    public enum WeaponAnimationType { Unarmed, Blade, Bow, Staff, Axe, Pickaxe }

    public override void _Ready() => Apply(new Appearance
    {
        SkinTone = SkinTone,
        HairColor = HairColor,
        ShirtColor = ShirtColor,
        LegColor = LegColor,
        BodyType = BodyType,
    });

    public void Apply(Appearance appearance)
    {
        _appearance = appearance;
        SkinTone = appearance.SkinTone;
        HairColor = appearance.HairColor;
        ShirtColor = appearance.ShirtColor;
        LegColor = appearance.LegColor;
        BodyType = appearance.BodyType;

        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _weapon = _shield = null;

        var broad = appearance.BodyType == "broad";
        var feminine = appearance.Presentation == "feminine";
        var chest = (broad ? 0.42f : 0.34f) - (feminine ? 0.025f : 0.0f) + appearance.TorsoStyle * 0.02f;
        var waist = chest * 0.72f;
        var armRadius = 0.10f + appearance.ArmStyle * 0.015f;
        var legRadius = 0.13f + appearance.LegStyle * 0.015f;
        var headRadius = 0.26f + appearance.HeadStyle * 0.016f;
        var shoulderX = chest + armRadius * 0.6f;

        // Tapered torso: an inverted frustum (chest wider than waist), faceted (Part 27.1).
        _torsoMesh = AddMesh(this, "Torso",
            new CylinderMesh { TopRadius = chest, BottomRadius = waist, Height = 0.92f, RadialSegments = Facets + 2, Rings = 1 },
            new Vector3(0, 1.25f, 0), appearance.ShirtColor);
        _torso = _torsoMesh;

        // Head with sharp nose + jaw ridges instead of a smooth ball (Part 27.1).
        _head = AddMesh(this, "Head",
            new SphereMesh { Radius = headRadius, Height = headRadius * 2.1f, RadialSegments = Facets + 2, Rings = 4 },
            new Vector3(0, 2.02f, 0), appearance.SkinTone);
        AddMesh(_head, "Nose", new PrismMesh { Size = new Vector3(0.07f, 0.10f, 0.12f) },
            new Vector3(0, -0.02f, headRadius * 0.92f), appearance.SkinTone, rotation: new Vector3(Mathf.Pi / 2, 0, 0));
        AddMesh(_head, "Jaw", new PrismMesh { Size = new Vector3(headRadius * 1.3f, 0.16f, headRadius * 0.9f) },
            new Vector3(0, -headRadius * 0.72f, headRadius * 0.18f), appearance.SkinTone, rotation: new Vector3(Mathf.Pi, 0, 0));
        AddHair(_head, appearance, headRadius);

        _leftShoulder = BuildArm("Left", new Vector3(-shoulderX, 1.6f, 0), armRadius, appearance.SkinTone, out _leftElbow, out _leftHandSocket);
        _rightShoulder = BuildArm("Right", new Vector3(shoulderX, 1.6f, 0), armRadius, appearance.SkinTone, out _rightElbow, out _rightHandSocket);
        _leftHip = BuildLeg("Left", new Vector3(-0.2f, 0.92f, 0), legRadius, appearance.LegColor, appearance.FootColor, appearance.FootStyle, out _leftKnee, out _legArmorAnchorL);
        _rightHip = BuildLeg("Right", new Vector3(0.2f, 0.92f, 0), legRadius, appearance.LegColor, appearance.FootColor, appearance.FootStyle, out _rightKnee, out _legArmorAnchorR);
    }

    /// <summary>Socket a weapon to the right hand and (optionally) a shield to the left hand.</summary>
    public void ApplyEquipment(string? weaponItemId, string? shieldItemId = null)
    {
        _weaponAnimation = WeaponTypeFor(weaponItemId);
        FreeChild(ref _weapon);
        if (_rightHandSocket is not null && !string.IsNullOrWhiteSpace(weaponItemId))
        {
            var mesh = _weaponAnimation switch
            {
                WeaponAnimationType.Bow => (PrimitiveMesh)new BoxMesh { Size = new Vector3(0.06f, 1.15f, 0.30f) },
                WeaponAnimationType.Staff => new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = 1.5f, RadialSegments = Facets },
                WeaponAnimationType.Axe => new BoxMesh { Size = new Vector3(0.34f, 1.0f, 0.1f) },
                WeaponAnimationType.Pickaxe => new BoxMesh { Size = new Vector3(0.5f, 1.0f, 0.1f) },
                _ => new BoxMesh { Size = new Vector3(0.09f, 0.95f, 0.09f) },
            };
            _weapon = AddMesh(_rightHandSocket, "Weapon", mesh, new Vector3(0, 0.32f, 0), "#c6a45a");
        }

        ApplyShield(shieldItemId);
    }

    /// <summary>Socket a low-poly shield to the left hand (Part 29: rigid weapon/shield attachment).</summary>
    public void ApplyShield(string? shieldItemId)
    {
        FreeChild(ref _shield);
        if (_leftHandSocket is null || string.IsNullOrWhiteSpace(shieldItemId))
        {
            return;
        }

        _shield = AddMesh(_leftHandSocket, "Shield",
            new CylinderMesh { TopRadius = 0.26f, BottomRadius = 0.26f, Height = 0.06f, RadialSegments = Facets },
            new Vector3(-0.12f, 0.05f, 0), "#7a5a32", rotation: new Vector3(0, 0, Mathf.Pi / 2));
    }

    /// <summary>
    /// Component replacement (Part 29.1): worn armour swaps the affected body sub-mesh for a bulkier
    /// plated variant instead of layering clothes. Procedural variants stand in until modelled tier
    /// meshes exist. <paramref name="slot"/> is "body" or "legs"; null/empty restores the base look.
    /// </summary>
    public void ApplyArmor(string slot, string? itemId)
    {
        var plated = !string.IsNullOrWhiteSpace(itemId);
        var tint = ArmorTint(itemId);

        if (slot == "body" && _torsoMesh is not null)
        {
            var chest = (_appearance.BodyType == "broad" ? 0.42f : 0.34f) + (plated ? 0.05f : 0.0f);
            _torsoMesh.Mesh = new CylinderMesh
            {
                TopRadius = chest,
                BottomRadius = chest * (plated ? 0.82f : 0.72f),
                Height = 0.92f,
                RadialSegments = Facets + 2,
                Rings = 1,
            };
            Recolor(_torsoMesh, plated ? tint : _appearance.ShirtColor);
        }
        else if (slot == "legs")
        {
            Recolor(_legArmorAnchorL, plated ? tint : _appearance.LegColor);
            Recolor(_legArmorAnchorR, plated ? tint : _appearance.LegColor);
        }
    }

    public override void _Process(double delta)
    {
        _animationTime += (float)delta;
        _attackRemaining = Mathf.Max(0.0f, _attackRemaining - (float)delta);
        ApplyProceduralPose();
    }

    public void SetLocomotion(bool isMoving, float speed) =>
        _movementBlend = isMoving ? Mathf.Clamp(speed / 5.0f, 0.75f, 1.55f) : 0.0f;

    public void PlayAttack(string? weaponItemId)
    {
        _weaponAnimation = WeaponTypeFor(weaponItemId);
        _attackRemaining = AttackDuration;
    }

    private void ApplyProceduralPose()
    {
        var walk = Mathf.Sin(_animationTime * 8.0f * Mathf.Max(1.0f, _movementBlend));
        var idle = Mathf.Sin(_animationTime * 2.1f);
        var attackProgress = _attackRemaining > 0.0f ? 1.0f - _attackRemaining / AttackDuration : 0.0f;
        var attackArc = _attackRemaining > 0.0f ? Mathf.Sin(attackProgress * Mathf.Pi) : 0.0f;

        Rotate(_leftShoulder, new Vector3(walk * 0.55f * _movementBlend, 0, 0));
        Rotate(_rightShoulder, new Vector3(-walk * 0.55f * _movementBlend, 0, 0));
        Rotate(_leftHip, new Vector3(-walk * 0.5f * _movementBlend, 0, 0));
        Rotate(_rightHip, new Vector3(walk * 0.5f * _movementBlend, 0, 0));
        // Rigid joint bend (Part 27.2): elbows/knees pivot a little, no smooth weighting.
        Rotate(_leftElbow, new Vector3(-0.12f - Mathf.Max(0, walk) * 0.35f * _movementBlend, 0, 0));
        Rotate(_rightElbow, new Vector3(-0.12f - Mathf.Max(0, -walk) * 0.35f * _movementBlend, 0, 0));
        Rotate(_leftKnee, new Vector3(0.12f + Mathf.Max(0, -walk) * 0.55f * _movementBlend, 0, 0));
        Rotate(_rightKnee, new Vector3(0.12f + Mathf.Max(0, walk) * 0.55f * _movementBlend, 0, 0));
        SetPosY(_torso, 1.25f + idle * 0.018f + Mathf.Abs(walk) * 0.025f * _movementBlend);
        SetPosY(_head, 2.02f + idle * 0.014f);

        if (_attackRemaining <= 0.0f)
        {
            return;
        }

        switch (_weaponAnimation)
        {
            case WeaponAnimationType.Bow:
                Rotate(_leftShoulder, new Vector3(-1.05f, 0.18f, -0.18f));
                Rotate(_rightShoulder, new Vector3(-0.92f, -0.52f * attackArc, 0.24f));
                break;
            case WeaponAnimationType.Staff:
                Rotate(_rightShoulder, new Vector3(-1.25f + attackArc * 0.55f, 0, -0.16f));
                Rotate(_weapon, new Vector3(-0.35f - attackArc * 0.5f, 0, 0));
                break;
            case WeaponAnimationType.Axe:
            case WeaponAnimationType.Pickaxe:
                Rotate(_rightShoulder, new Vector3(-2.0f + attackArc * 2.65f, 0, -0.2f));
                Rotate(_rightElbow, new Vector3(-0.2f - attackArc * 0.4f, 0, 0));
                break;
            case WeaponAnimationType.Blade:
                Rotate(_rightShoulder, new Vector3(-0.5f, 0, -0.4f + attackArc * 1.5f));
                Rotate(_weapon, new Vector3(0, 0, -0.3f + attackArc * 1.6f));
                break;
            default:
                Rotate(_rightShoulder, new Vector3(-0.45f - attackArc * 1.0f, 0, 0));
                break;
        }
    }

    // ---- Build helpers ----------------------------------------------------

    private Node3D BuildArm(string side, Vector3 shoulder, float radius, string skin, out Node3D elbow, out Node3D handSocket)
    {
        var shoulderPivot = new Node3D { Name = $"{side}Arm", Position = shoulder };
        AddChild(shoulderPivot);
        // Upper arm hangs from the shoulder pivot origin.
        AddMesh(shoulderPivot, "UpperArm", new CylinderMesh { TopRadius = radius, BottomRadius = radius * 0.86f, Height = 0.42f, RadialSegments = Facets }, new Vector3(0, -0.21f, 0), skin);

        elbow = new Node3D { Name = $"{side}Elbow", Position = new Vector3(0, -0.43f, 0) };
        shoulderPivot.AddChild(elbow);
        AddMesh(elbow, "LowerArm", new CylinderMesh { TopRadius = radius * 0.86f, BottomRadius = radius * 0.72f, Height = 0.42f, RadialSegments = Facets }, new Vector3(0, -0.21f, 0), skin);
        AddMesh(elbow, "Hand", new SphereMesh { Radius = radius * 1.05f, Height = radius * 2.1f, RadialSegments = Facets, Rings = 3 }, new Vector3(0, -0.45f, 0), skin);

        handSocket = new Node3D { Name = $"{side}HandSocket", Position = new Vector3(0, -0.5f, 0.04f) };
        elbow.AddChild(handSocket);
        return shoulderPivot;
    }

    private Node3D BuildLeg(string side, Vector3 hip, float radius, string legColor, string footColor, int footStyle, out Node3D knee, out MeshInstance3D thighMesh)
    {
        var hipPivot = new Node3D { Name = $"{side}Leg", Position = hip };
        AddChild(hipPivot);
        thighMesh = AddMesh(hipPivot, "Thigh", new CylinderMesh { TopRadius = radius, BottomRadius = radius * 0.86f, Height = 0.46f, RadialSegments = Facets }, new Vector3(0, -0.23f, 0), legColor);

        knee = new Node3D { Name = $"{side}Knee", Position = new Vector3(0, -0.46f, 0) };
        hipPivot.AddChild(knee);
        AddMesh(knee, "Shin", new CylinderMesh { TopRadius = radius * 0.86f, BottomRadius = radius * 0.7f, Height = 0.46f, RadialSegments = Facets }, new Vector3(0, -0.23f, 0), legColor);
        // Wedge-shaped foot block (Part 27.1).
        AddMesh(knee, "Foot", new BoxMesh { Size = new Vector3(0.26f, 0.2f, 0.46f + footStyle * 0.04f) }, new Vector3(0, -0.46f, -0.1f), footColor);
        return hipPivot;
    }

    private void AddHair(Node3D parent, Appearance appearance, float headRadius)
    {
        var hairHeight = appearance.HairStyle switch
        {
            0 => 0.16f, 1 => 0.26f, 2 => 0.38f, 3 => 0.50f, 4 => 0.32f, _ => 0.22f,
        };
        AddMesh(parent, "Hair",
            new SphereMesh { Radius = headRadius * 1.12f, Height = hairHeight, RadialSegments = Facets + 2, Rings = 3 },
            new Vector3(0, headRadius * 0.62f + hairHeight * 0.12f, -0.015f), appearance.HairColor);
    }

    private static WeaponAnimationType WeaponTypeFor(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return WeaponAnimationType.Unarmed;
        }

        var normalized = itemId.ToLowerInvariant();
        if (normalized.Contains("bow")) return WeaponAnimationType.Bow;
        if (normalized.Contains("staff") || normalized.Contains("catalyst")) return WeaponAnimationType.Staff;
        if (normalized.Contains("hatchet") || normalized.Contains("axe")) return WeaponAnimationType.Axe;
        if (normalized.Contains("pickaxe")) return WeaponAnimationType.Pickaxe;
        return WeaponAnimationType.Blade;
    }

    private static string ArmorTint(string? itemId)
    {
        var id = itemId?.ToLowerInvariant() ?? string.Empty;
        if (id.Contains("iron") || id.Contains("steel") || id.Contains("plate") || id.Contains("chain")) return "#9aa0ad";
        if (id.Contains("bronze")) return "#9a6a32";
        if (id.Contains("leather")) return "#6a4a2a";
        if (id.Contains("robe") || id.Contains("mind")) return "#3a4a8a";
        return "#8a8a92";
    }

    private static void Rotate(Node3D? node, Vector3 rotation)
    {
        if (node is not null)
        {
            node.Rotation = rotation;
        }
    }

    private static void SetPosY(Node3D? node, float y)
    {
        if (node is not null)
        {
            node.Position = new Vector3(node.Position.X, y, node.Position.Z);
        }
    }

    private void FreeChild(ref Node3D? node)
    {
        if (node is not null)
        {
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
            node = null;
        }
    }

    private static void Recolor(MeshInstance3D? mesh, string color)
    {
        if (mesh?.GetSurfaceOverrideMaterial(0) is StandardMaterial3D material)
        {
            material.AlbedoColor = new Color(color);
        }
    }

    private MeshInstance3D AddMesh(Node3D parent, string name, Mesh mesh, Vector3 position, string color, Vector3 rotation = default)
    {
        var part = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = position,
            Rotation = rotation,
        };
        part.SetSurfaceOverrideMaterial(0, LowPolyMaterial(color));
        parent.AddChild(part);
        return part;
    }

    // Flat, no-PBR, point-filtered material (Part 28): diffuse only, no metallic/roughness sheen,
    // Nearest filtering so any future hand-painted 64/128px texture stays crisp and aliased.
    private static StandardMaterial3D LowPolyMaterial(string color) => new()
    {
        AlbedoColor = new Color(color),
        Roughness = 1.0f,
        Metallic = 0.0f,
        SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
    };
}
