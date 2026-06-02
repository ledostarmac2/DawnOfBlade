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
        GetNodeOrNull<Node3D>("Weapon")?.QueueFree();
        if (string.IsNullOrWhiteSpace(weaponItemId))
        {
            return;
        }

        AddPart("Weapon", new BoxMesh { Size = new Vector3(0.12f, 1.05f, 0.12f) }, new Vector3(0.62f, 1.05f, 0), "#c6a45a");
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
