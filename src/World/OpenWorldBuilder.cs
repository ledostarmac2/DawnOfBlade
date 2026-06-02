using System;
using Godot;

namespace DawnOfBlade.World;

/// <summary>Decorates the prototype map into a broad, explorable overworld with stable seeded scenery.</summary>
public partial class OpenWorldBuilder : Node3D
{
    [Export] public int Seed { get; set; } = 2026;
    [Export] public int TreeCount { get; set; } = 90;
    [Export] public int RockCount { get; set; } = 48;
    [Export] public float Radius { get; set; } = 82.0f;

    public override void _Ready()
    {
        var random = new Random(Seed);
        for (var i = 0; i < TreeCount; i++)
        {
            var position = NextPosition(random);
            if (position.Length() > 15)
            {
                AddTree(position, 0.8f + (float)random.NextDouble() * 0.9f);
            }
        }

        for (var i = 0; i < RockCount; i++)
        {
            var position = NextPosition(random);
            if (position.Length() > 12)
            {
                AddRock(position, 0.35f + (float)random.NextDouble() * 0.7f);
            }
        }
    }

    private Vector3 NextPosition(Random random) =>
        new(((float)random.NextDouble() * 2 - 1) * Radius, 0, ((float)random.NextDouble() * 2 - 1) * Radius);

    private void AddTree(Vector3 position, float scale)
    {
        var root = new Node3D { Position = position, Scale = Vector3.One * scale };
        AddChild(root);
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.32f, Height = 2.8f }, new Vector3(0, 1.4f, 0), "#654321"));
        root.AddChild(Part(new SphereMesh { Radius = 1.05f, Height = 2.1f }, new Vector3(0, 3.25f, 0), "#285b32"));
    }

    private void AddRock(Vector3 position, float scale)
    {
        var rock = Part(new SphereMesh { Radius = 0.8f, Height = 1.1f }, position + Vector3.Up * 0.5f, "#66706f");
        rock.Scale = new Vector3(scale * 1.4f, scale, scale);
        AddChild(rock);
    }

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, string color)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position };
        part.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = new Color(color) });
        return part;
    }
}
