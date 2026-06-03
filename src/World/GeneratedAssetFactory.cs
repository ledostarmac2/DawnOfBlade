using Godot;

namespace DawnOfBlade.World;

/// <summary>Creates lightweight low-poly visuals for world entities without external model dependencies.</summary>
public static class GeneratedAssetFactory
{
    public static Node3D CreateResource(string itemId, int visualVariant = 0) =>
        itemId switch
        {
            "logs" => Tree("#684321", "#2f6b36", visualVariant),
            "copper_ore" => Ore("#88513e", "#d4845e"),
            "tin_ore" => Ore("#647070", "#c4d0ca"),
            "raw_trout" => FishingSpot(),
            _ => Herb("#d6a833", "#6fa83b"),
        };

    public static Node3D CreateHostile(string entityId, string color) =>
        entityId switch
        {
            "reanimated_skeleton" => Skeleton(),
            "forest_marauder" => Marauder(color),
            "training_dummy" => TrainingDummy(),
            _ => Rodent(),
        };

    public static Node3D TrainingDummy()
    {
        var root = new Node3D { Name = "GeneratedTrainingDummy" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.24f, Height = 1.62f, RadialSegments = 8 }, new Vector3(0, 0.42f, 0), "#7a4d2e"));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.46f, BottomRadius = 0.42f, Height = 0.68f, RadialSegments = 8 }, new Vector3(0, 0.92f, 0), "#9b6b3f"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(1.18f, 0.14f, 0.18f) }, new Vector3(0, 1.06f, -0.02f), "#5b3824"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.36f, 0.42f, 0.06f) }, new Vector3(0, 1.0f, -0.43f), "#b74436"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.52f, 0.08f, 0.07f) }, new Vector3(0, 1.18f, -0.46f), "#d7bf74"));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.26f, BottomRadius = 0.30f, Height = 0.18f, RadialSegments = 8 }, new Vector3(0, 1.36f, 0), "#c9a56c"));
        root.AddChild(Part(new TorusMesh { InnerRadius = 0.25f, OuterRadius = 0.32f, Rings = 8, RingSegments = 5 }, new Vector3(0, 0.93f, 0), "#5b3824", new Vector3(Mathf.Pi / 2, 0, 0)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.62f, 0.06f) }, new Vector3(-0.22f, 0.98f, -0.47f), "#d4c078"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.62f, 0.06f) }, new Vector3(0.22f, 0.98f, -0.47f), "#d4c078"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.9f, 0.12f, 0.58f) }, new Vector3(0, 0.06f, 0), "#3a2a20"));
        return root;
    }

    public static Node3D Tree(string trunkColor = "#654321", string leafColor = "#285b32", int visualVariant = 0)
    {
        var variant = visualVariant <= 0 ? 4 : Mathf.PosMod(visualVariant - 1, 36) + 1;
        var heightBias = 1.0f + (variant % 5 - 2) * 0.035f;
        var canopyBias = 1.0f + (variant % 7 - 3) * 0.025f;
        var leafMaterial = ArtMaterialCatalog.TreeLeaves(leafColor, variant);
        var root = new Node3D { Name = "GeneratedTree" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.38f, Height = 2.8f * heightBias, RadialSegments = 8 }, new Vector3(0, 1.4f * heightBias, 0), trunkColor));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.24f, BottomRadius = 0.42f, Height = 0.09f, RadialSegments = 8 }, new Vector3(0, 0.55f, 0), Darken(trunkColor, 0.78f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.20f, BottomRadius = 0.34f, Height = 0.075f, RadialSegments = 8 }, new Vector3(0, 1.18f, 0), Lighten(trunkColor, 1.16f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.30f, Height = 0.065f, RadialSegments = 8 }, new Vector3(0, 1.84f, 0), Darken(trunkColor, 0.86f)));
        for (var i = 0; i < 4; i++)
        {
            var angle = i * Mathf.Pi * 0.5f + 0.28f;
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.38f;
            root.AddChild(Part(
                new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.11f, Height = 0.9f, RadialSegments = 5 },
                new Vector3(offset.X, 0.16f, offset.Z), Darken(trunkColor, 0.72f),
                new Vector3(Mathf.Pi / 2, angle, 0.0f)));
        }
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.18f, Height = 1.1f, RadialSegments = 6 }, new Vector3(0.36f, 2.35f, 0.08f), trunkColor, new Vector3(0.56f, 0, -0.38f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.16f, Height = 0.95f, RadialSegments = 6 }, new Vector3(-0.34f, 2.18f, -0.12f), trunkColor, new Vector3(0.48f, 0, 0.42f)));
        root.AddChild(Part(new SphereMesh { Radius = 0.24f, Height = 0.18f, RadialSegments = 7, Rings = 3 }, new Vector3(0.42f, 0.12f, 0.16f), "#4b2e1e"));
        root.AddChild(Part(new SphereMesh { Radius = 1.08f * canopyBias, Height = 1.8f * canopyBias, RadialSegments = 9, Rings = 5 }, new Vector3(0, 3.05f * heightBias, 0), leafMaterial));
        root.AddChild(Part(new SphereMesh { Radius = 0.84f * canopyBias, Height = 1.35f * canopyBias, RadialSegments = 8, Rings = 4 }, new Vector3(0.68f, 3.2f * heightBias, 0.1f), leafMaterial));
        root.AddChild(Part(new SphereMesh { Radius = 0.78f * canopyBias, Height = 1.28f * canopyBias, RadialSegments = 8, Rings = 4 }, new Vector3(-0.64f, 3.16f * heightBias, -0.08f), leafMaterial));
        root.AddChild(Part(new SphereMesh { Radius = 0.62f * canopyBias, Height = 1.0f * canopyBias, RadialSegments = 7, Rings = 4 }, new Vector3(0.08f, 3.72f * heightBias, -0.28f), leafMaterial));
        root.AddChild(Part(new SphereMesh { Radius = 0.42f * canopyBias, Height = 0.7f * canopyBias, RadialSegments = 7, Rings = 3 }, new Vector3(-0.22f, 2.66f * heightBias, -0.74f), leafMaterial));
        root.AddChild(Part(new SphereMesh { Radius = 0.36f * canopyBias, Height = 0.58f * canopyBias, RadialSegments = 7, Rings = 3 }, new Vector3(0.82f, 2.76f * heightBias, -0.56f), leafMaterial));
        return root;
    }

    public static Node3D Bush(int visualVariant = 1, string foliageColor = "#3f7a3a")
    {
        var variant = visualVariant <= 0 ? 1 : Mathf.PosMod(visualVariant - 1, 8) + 1;
        var leaf = ArtMaterialCatalog.BushLeaves(foliageColor, variant);
        var spread = 1.0f + (variant % 4 - 1.5f) * 0.06f;
        var root = new Node3D { Name = "GeneratedBush" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.13f, Height = 0.34f, RadialSegments = 6 }, new Vector3(0, 0.17f, 0), "#5b3a24"));
        root.AddChild(Part(new SphereMesh { Radius = 0.52f * spread, Height = 0.82f, RadialSegments = 8, Rings = 4 }, new Vector3(0, 0.55f, 0), leaf));
        root.AddChild(Part(new SphereMesh { Radius = 0.38f * spread, Height = 0.6f, RadialSegments = 7, Rings = 4 }, new Vector3(0.34f, 0.46f, 0.08f), leaf));
        root.AddChild(Part(new SphereMesh { Radius = 0.34f * spread, Height = 0.54f, RadialSegments = 7, Rings = 4 }, new Vector3(-0.3f, 0.44f, -0.06f), leaf));
        root.AddChild(Part(new SphereMesh { Radius = 0.28f * spread, Height = 0.46f, RadialSegments = 7, Rings = 3 }, new Vector3(0.04f, 0.74f, -0.2f), leaf));
        return root;
    }

    public static Node3D Ore(string stoneColor, string veinColor)
    {
        var root = new Node3D { Name = "GeneratedOre" };
        AddRock(root, new Vector3(0, 0.48f, 0), new Vector3(1.15f, 0.8f, 0.9f), stoneColor);
        AddRock(root, new Vector3(0.55f, 0.42f, 0.12f), new Vector3(0.62f, 0.58f, 0.55f), veinColor);
        AddRock(root, new Vector3(-0.44f, 0.34f, -0.1f), new Vector3(0.48f, 0.42f, 0.52f), veinColor);
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.78f, 0.06f, 0.05f) }, new Vector3(0.18f, 0.78f, -0.45f), Lighten(veinColor, 1.22f), new Vector3(0.2f, 0.42f, -0.16f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.56f, 0.05f, 0.045f) }, new Vector3(-0.36f, 0.62f, 0.46f), Lighten(veinColor, 1.12f), new Vector3(-0.1f, -0.55f, 0.18f)));
        root.AddChild(Part(new PrismMesh { Size = new Vector3(0.18f, 0.62f, 0.18f) }, new Vector3(0.18f, 0.92f, -0.2f), Lighten(veinColor, 1.2f), new Vector3(0.18f, 0.35f, 0.12f)));
        root.AddChild(Part(new PrismMesh { Size = new Vector3(0.12f, 0.46f, 0.12f) }, new Vector3(-0.22f, 0.78f, 0.22f), Lighten(veinColor, 1.1f), new Vector3(-0.14f, -0.2f, -0.18f)));
        return root;
    }

    public static Node3D Herb(string flowerColor, string leafColor)
    {
        var root = new Node3D { Name = "GeneratedHerb" };
        for (var i = -2; i <= 2; i++)
        {
            var blade = Part(new BoxMesh { Size = new Vector3(0.12f, 0.6f, 0.12f) }, new Vector3(i * 0.14f, 0.3f, (i % 2) * 0.08f), leafColor);
            blade.Rotation = new Vector3(0.12f * i, i * 0.24f, 0.08f * i);
            root.AddChild(blade);
            root.AddChild(Part(new SphereMesh { Radius = 0.12f, Height = 0.24f, RadialSegments = 6, Rings = 3 }, new Vector3(i * 0.14f, 0.66f, (i % 2) * 0.08f), flowerColor));
        }
        return root;
    }

    public static Node3D FishingSpot()
    {
        var root = new Node3D { Name = "GeneratedFishingSpot" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.72f, BottomRadius = 0.72f, Height = 0.04f, RadialSegments = 16 }, new Vector3(0, 0.03f, 0), WaterMaterial()));
        root.AddChild(Part(new TorusMesh { InnerRadius = 0.42f, OuterRadius = 0.58f, Rings = 12, RingSegments = 6 }, new Vector3(0, 0.08f, 0), "#b4d8df"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.04f, 0.018f, 0.92f) }, new Vector3(-0.18f, 0.12f, 0.08f), "#eef8fb", new Vector3(0, 0.35f, 0)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.035f, 0.016f, 0.66f) }, new Vector3(0.25f, 0.13f, 0.1f), "#c9eef4", new Vector3(0, -0.42f, 0)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.08f, 0.68f) }, new Vector3(0.22f, 0.11f, -0.12f), "#e7d7a4", new Vector3(0, 0.55f, 0)));
        return root;
    }

    public static MeshInstance3D Rock(Vector3 position, float scale, string color)
    {
        var rock = Part(new SphereMesh { Radius = 0.8f, Height = 1.1f, RadialSegments = 6, Rings = 3 }, position + Vector3.Up * 0.5f, color);
        rock.Scale = new Vector3(scale * 1.4f, scale, scale);
        rock.Rotation = new Vector3(0.0f, scale * 0.9f, 0.06f);
        return rock;
    }

    private static Node3D Skeleton()
    {
        var root = new Node3D { Name = "GeneratedSkeleton" };
        root.AddChild(Part(new SphereMesh { Radius = 0.25f, Height = 0.5f, RadialSegments = 7, Rings = 4 }, new Vector3(0, 1.0f, 0), "#dad1b2"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.1f, 0.58f, 0.08f) }, new Vector3(0, 0.48f, 0), "#d8d0b2"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.62f, 0.08f, 0.1f) }, new Vector3(0, 0.73f, 0), "#d8d0b2"));
        for (var i = 0; i < 4; i++)
        {
            root.AddChild(Part(new BoxMesh { Size = new Vector3(0.52f - i * 0.06f, 0.045f, 0.08f) }, new Vector3(0, 0.66f - i * 0.105f, -0.01f), "#c3b990"));
        }
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.42f, 0.13f, 0.16f) }, new Vector3(0, 0.18f, 0), "#c3b990"));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.045f, BottomRadius = 0.06f, Height = 0.62f, RadialSegments = 5 }, new Vector3(-0.42f, 0.46f, 0), "#d8d0b2", new Vector3(0, 0, -0.25f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.045f, BottomRadius = 0.06f, Height = 0.62f, RadialSegments = 5 }, new Vector3(0.42f, 0.46f, 0), "#d8d0b2", new Vector3(0, 0, 0.25f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.065f, Height = 0.64f, RadialSegments = 5 }, new Vector3(-0.16f, -0.14f, 0), "#d8d0b2", new Vector3(0.12f, 0, 0.08f)));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.065f, Height = 0.64f, RadialSegments = 5 }, new Vector3(0.16f, -0.14f, 0), "#d8d0b2", new Vector3(0.12f, 0, -0.08f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.07f, 0.065f, 0.03f) }, new Vector3(-0.09f, 1.06f, -0.23f), "#111111"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.07f, 0.065f, 0.03f) }, new Vector3(0.09f, 1.06f, -0.23f), "#111111"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.15f, 0.035f, 0.025f) }, new Vector3(0, 0.91f, -0.25f), "#5f5645"));
        root.AddChild(Part(new PrismMesh { Size = new Vector3(0.18f, 0.62f, 0.07f) }, new Vector3(0.52f, 0.34f, -0.08f), "#aeb5b6", new Vector3(0, 0, -0.36f)));
        return root;
    }

    private static Node3D Marauder(string color)
    {
        var root = new Node3D { Name = "GeneratedMarauder" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.34f, BottomRadius = 0.26f, Height = 0.82f, RadialSegments = 8 }, new Vector3(0, 0.35f, 0), color));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.7f, 0.14f, 0.34f) }, new Vector3(0, 0.78f, 0), "#4b3323"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.5f, 0.38f, 0.06f) }, new Vector3(0, 0.38f, -0.28f), "#6f412d"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.72f, 0.06f) }, new Vector3(-0.18f, 0.4f, -0.34f), "#2a2118", new Vector3(0, 0, -0.28f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.72f, 0.06f) }, new Vector3(0.18f, 0.4f, -0.34f), "#2a2118", new Vector3(0, 0, 0.28f)));
        root.AddChild(Part(new SphereMesh { Radius = 0.27f, Height = 0.54f, RadialSegments = 7, Rings = 4 }, new Vector3(0, 0.98f, 0), "#7d9b55"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.54f, 0.14f, 0.26f) }, new Vector3(0, 1.15f, -0.02f), "#322419"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.6f, 0.1f) }, new Vector3(-0.48f, 0.42f, 0), "#7d9b55", new Vector3(0, 0, -0.22f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.6f, 0.1f) }, new Vector3(0.48f, 0.42f, 0), "#7d9b55", new Vector3(0, 0, 0.22f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.13f, 0.66f, 0.11f) }, new Vector3(-0.17f, -0.22f, 0), "#3a322a"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.13f, 0.66f, 0.11f) }, new Vector3(0.17f, -0.22f, 0), "#3a322a"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.14f, 0.85f, 0.12f) }, new Vector3(0.64f, 0.1f, -0.12f), "#b6a06a", new Vector3(0, 0, -0.24f)));
        root.AddChild(Part(new PrismMesh { Size = new Vector3(0.22f, 0.42f, 0.08f) }, new Vector3(0.76f, 0.54f, -0.12f), "#c3c8c8", new Vector3(0, 0, 0.1f)));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.05f, 0.03f) }, new Vector3(-0.09f, 1.05f, -0.25f), "#101010"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.05f, 0.03f) }, new Vector3(0.09f, 1.05f, -0.25f), "#101010"));
        return root;
    }

    private static Node3D Rodent()
    {
        var root = new Node3D { Name = "GeneratedRodent" };
        root.AddChild(Part(new SphereMesh { Radius = 0.42f, Height = 0.62f, RadialSegments = 7, Rings = 4 }, new Vector3(0, -0.28f, 0), "#765748"));
        root.AddChild(Part(new SphereMesh { Radius = 0.26f, Height = 0.4f, RadialSegments = 7, Rings = 4 }, new Vector3(0, -0.2f, -0.42f), "#876657"));
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.055f, Height = 0.66f, RadialSegments = 5 }, new Vector3(0, -0.24f, 0.52f), "#5e4035", new Vector3(Mathf.Pi / 2, 0, 0)));
        root.AddChild(Part(new SphereMesh { Radius = 0.09f, Height = 0.05f, RadialSegments = 5, Rings = 3 }, new Vector3(-0.18f, -0.02f, -0.57f), "#6a4a3f"));
        root.AddChild(Part(new SphereMesh { Radius = 0.09f, Height = 0.05f, RadialSegments = 5, Rings = 3 }, new Vector3(0.18f, -0.02f, -0.57f), "#6a4a3f"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.16f, 0.035f, 0.06f) }, new Vector3(0, -0.22f, -0.72f), "#d9c5af"));
        root.AddChild(Part(new SphereMesh { Radius = 0.055f, Height = 0.06f, RadialSegments = 5, Rings = 3 }, new Vector3(-0.09f, -0.13f, -0.65f), "#111111"));
        root.AddChild(Part(new SphereMesh { Radius = 0.055f, Height = 0.06f, RadialSegments = 5, Rings = 3 }, new Vector3(0.09f, -0.13f, -0.65f), "#111111"));
        return root;
    }

    private static void AddRock(Node3D root, Vector3 position, Vector3 scale, string color)
    {
        var rock = Part(new SphereMesh { Radius = 0.75f, Height = 1.0f, RadialSegments = 6, Rings = 3 }, position, color);
        rock.Scale = scale;
        root.AddChild(rock);
    }

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, string color, Vector3 rotation = default)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation };
        part.SetSurfaceOverrideMaterial(0, NaturalMaterial(color));
        return part;
    }

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, Material material, Vector3 rotation = default)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation };
        part.SetSurfaceOverrideMaterial(0, material);
        return part;
    }

    private static StandardMaterial3D NaturalMaterial(string color) =>
        ArtMaterialCatalog.Environment(color);

    private static StandardMaterial3D WaterMaterial() =>
        ArtMaterialCatalog.Water(new Color(0.18f, 0.55f, 0.68f, 0.72f));

    private static string Lighten(string color, float factor) => ScaleColor(color, factor);

    private static string Darken(string color, float factor) => ScaleColor(color, factor);

    private static string ScaleColor(string color, float factor)
    {
        var source = new Color(color);
        return new Color(
            Mathf.Clamp(source.R * factor, 0.0f, 1.0f),
            Mathf.Clamp(source.G * factor, 0.0f, 1.0f),
            Mathf.Clamp(source.B * factor, 0.0f, 1.0f),
            source.A).ToHtml();
    }
}
