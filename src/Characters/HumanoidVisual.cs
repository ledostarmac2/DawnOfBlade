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
        AddPart("Torso", new BoxMesh { Size = new Vector3(broad ? 0.82f : 0.66f, 0.92f, 0.36f) }, new Vector3(0, 1.25f, 0), appearance.ShirtColor);
        AddPart("Head", new SphereMesh { Radius = 0.29f, Height = 0.58f }, new Vector3(0, 2.02f, 0), appearance.SkinTone);
        AddPart("Hair", new SphereMesh { Radius = 0.305f, Height = 0.28f }, new Vector3(0, 2.19f, -0.015f), appearance.HairColor);
        AddLimb("LeftArm", new Vector3(-0.48f, 1.25f, 0), appearance.SkinTone);
        AddLimb("RightArm", new Vector3(0.48f, 1.25f, 0), appearance.SkinTone);
        AddLeg("LeftLeg", new Vector3(-0.2f, 0.48f, 0), appearance.LegColor);
        AddLeg("RightLeg", new Vector3(0.2f, 0.48f, 0), appearance.LegColor);
    }

    private void AddLimb(string name, Vector3 position, string color) =>
        AddPart(name, new CapsuleMesh { Radius = 0.12f, Height = 0.86f }, position, color);

    private void AddLeg(string name, Vector3 position, string color) =>
        AddPart(name, new CapsuleMesh { Radius = 0.15f, Height = 0.96f }, position, color);

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
