using Godot;

namespace DawnOfBlade.Characters;

/// <summary>Builds a lightweight humanoid from primitives so actors read as people before final art lands.</summary>
public partial class HumanoidVisual : Node3D
{
    [Export] public string SkinTone { get; set; } = "#e0b48c";
    [Export] public string HairColor { get; set; } = "#3a2a1a";
    [Export] public string ShirtColor { get; set; } = "#6a5acd";
    [Export] public string LegColor { get; set; } = "#3b3b46";
    [Export] public string BodyType { get; set; } = "slim";

    private const float AttackDuration = 0.52f;

    private float _animationTime;
    private float _movementBlend;
    private float _attackRemaining;
    private WeaponAnimationType _weaponAnimation = WeaponAnimationType.Unarmed;

    public enum WeaponAnimationType
    {
        Unarmed,
        Blade,
        Bow,
        Staff,
        Axe,
        Pickaxe,
    }

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

        var broad = appearance.BodyType == "broad";
        var feminine = appearance.Presentation == "feminine";
        var torsoWidth = (broad ? 0.82f : 0.66f) - (feminine ? 0.05f : 0.0f) + appearance.TorsoStyle * 0.045f;
        var armRadius = 0.105f + appearance.ArmStyle * 0.018f;
        var legRadius = 0.14f + appearance.LegStyle * 0.018f;
        var headRadius = 0.27f + appearance.HeadStyle * 0.018f;

        AddPart("Torso", new BoxMesh { Size = new Vector3(torsoWidth, 0.92f, 0.36f) }, new Vector3(0, 1.25f, 0), appearance.ShirtColor);
        AddPart("Head", new SphereMesh { Radius = headRadius, Height = 0.58f + appearance.JawStyle * 0.035f }, new Vector3(0, 2.02f, 0), appearance.SkinTone);
        AddHair(appearance);
        AddLimb("LeftArm", new Vector3(-0.48f, 1.25f, 0), appearance.SkinTone, armRadius);
        AddLimb("RightArm", new Vector3(0.48f, 1.25f, 0), appearance.SkinTone, armRadius);
        AddPart("LeftHand", new SphereMesh { Radius = 0.11f + appearance.HandStyle * 0.012f, Height = 0.22f }, new Vector3(-0.48f, 0.78f, 0), appearance.SkinTone);
        AddPart("RightHand", new SphereMesh { Radius = 0.11f + appearance.HandStyle * 0.012f, Height = 0.22f }, new Vector3(0.48f, 0.78f, 0), appearance.SkinTone);
        AddLeg("LeftLeg", new Vector3(-0.2f, 0.48f, 0), appearance.LegColor, legRadius);
        AddLeg("RightLeg", new Vector3(0.2f, 0.48f, 0), appearance.LegColor, legRadius);
        AddPart("LeftFoot", new BoxMesh { Size = new Vector3(0.28f, 0.22f, 0.48f + appearance.FootStyle * 0.05f) }, new Vector3(-0.2f, 0.03f, -0.1f), appearance.FootColor);
        AddPart("RightFoot", new BoxMesh { Size = new Vector3(0.28f, 0.22f, 0.48f + appearance.FootStyle * 0.05f) }, new Vector3(0.2f, 0.03f, -0.1f), appearance.FootColor);
    }

    public void ApplyEquipment(string? weaponItemId)
    {
        if (GetNodeOrNull<Node3D>("Weapon") is { } oldWeapon)
        {
            RemoveChild(oldWeapon);
            oldWeapon.QueueFree();
        }
        _weaponAnimation = WeaponTypeFor(weaponItemId);
        if (string.IsNullOrWhiteSpace(weaponItemId))
        {
            return;
        }

        var mesh = _weaponAnimation switch
        {
            WeaponAnimationType.Bow => new BoxMesh { Size = new Vector3(0.08f, 1.15f, 0.34f) },
            WeaponAnimationType.Staff => new BoxMesh { Size = new Vector3(0.10f, 1.5f, 0.10f) },
            WeaponAnimationType.Axe => new BoxMesh { Size = new Vector3(0.38f, 1.1f, 0.12f) },
            WeaponAnimationType.Pickaxe => new BoxMesh { Size = new Vector3(0.52f, 1.1f, 0.12f) },
            _ => new BoxMesh { Size = new Vector3(0.12f, 1.05f, 0.12f) },
        };
        AddPart("Weapon", mesh, new Vector3(0.62f, 1.05f, 0), "#c6a45a");
    }

    public override void _Process(double delta)
    {
        _animationTime += (float)delta;
        _attackRemaining = Mathf.Max(0.0f, _attackRemaining - (float)delta);
        ApplyProceduralPose();
    }

    public void SetLocomotion(bool isMoving, float speed)
    {
        _movementBlend = isMoving ? Mathf.Clamp(speed / 5.0f, 0.75f, 1.55f) : 0.0f;
    }

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

        SetRotation("LeftArm", new Vector3(walk * 0.55f * _movementBlend, 0, 0));
        SetRotation("RightArm", new Vector3(-walk * 0.55f * _movementBlend, 0, 0));
        SetRotation("LeftLeg", new Vector3(-walk * 0.5f * _movementBlend, 0, 0));
        SetRotation("RightLeg", new Vector3(walk * 0.5f * _movementBlend, 0, 0));
        SetPositionY("Torso", 1.25f + idle * 0.018f + Mathf.Abs(walk) * 0.025f * _movementBlend);
        SetPositionY("Head", 2.02f + idle * 0.014f);

        if (_attackRemaining <= 0.0f)
        {
            return;
        }

        switch (_weaponAnimation)
        {
            case WeaponAnimationType.Bow:
                SetRotation("LeftArm", new Vector3(-1.05f, 0.18f, -0.18f));
                SetRotation("RightArm", new Vector3(-0.92f, -0.52f * attackArc, 0.24f));
                SetRotation("Weapon", new Vector3(0, 0, -0.3f));
                break;
            case WeaponAnimationType.Staff:
                SetRotation("RightArm", new Vector3(-1.25f + attackArc * 0.55f, 0, -0.16f));
                SetRotation("Weapon", new Vector3(-0.35f - attackArc * 0.5f, 0, 0));
                break;
            case WeaponAnimationType.Axe:
            case WeaponAnimationType.Pickaxe:
                SetRotation("RightArm", new Vector3(-2.0f + attackArc * 2.65f, 0, -0.2f));
                SetRotation("Weapon", new Vector3(-0.8f + attackArc * 1.8f, 0, 0));
                break;
            case WeaponAnimationType.Blade:
                SetRotation("RightArm", new Vector3(-0.5f, 0, -0.4f + attackArc * 1.5f));
                SetRotation("Weapon", new Vector3(0, 0, -0.3f + attackArc * 1.6f));
                break;
            default:
                SetRotation("RightArm", new Vector3(-0.45f - attackArc * 1.0f, 0, 0));
                break;
        }
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

    private void SetRotation(string nodeName, Vector3 rotation)
    {
        if (GetNodeOrNull<Node3D>(nodeName) is { } node)
        {
            node.Rotation = rotation;
        }
    }

    private void SetPositionY(string nodeName, float y)
    {
        if (GetNodeOrNull<Node3D>(nodeName) is { } node)
        {
            node.Position = new Vector3(node.Position.X, y, node.Position.Z);
        }
    }

    private void AddHair(Appearance appearance)
    {
        var hairHeight = appearance.HairStyle switch
        {
            0 => 0.18f,
            1 => 0.28f,
            2 => 0.40f,
            3 => 0.52f,
            4 => 0.34f,
            _ => 0.24f,
        };
        AddPart("Hair", new SphereMesh { Radius = 0.305f, Height = hairHeight }, new Vector3(0, 2.19f + hairHeight * 0.12f, -0.015f), appearance.HairColor);
    }

    private void AddLimb(string name, Vector3 position, string color, float radius) =>
        AddPart(name, new CapsuleMesh { Radius = radius, Height = 0.86f }, position, color);

    private void AddLeg(string name, Vector3 position, string color, float radius) =>
        AddPart(name, new CapsuleMesh { Radius = radius, Height = 0.96f }, position, color);

    private void AddPart(string name, PrimitiveMesh mesh, Vector3 position, string color)
    {
        var part = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = position,
        };
        part.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = new Color(color) });
        AddChild(part);
    }
}
