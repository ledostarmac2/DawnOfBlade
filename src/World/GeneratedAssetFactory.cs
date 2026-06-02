using Godot;

namespace DawnOfBlade.World;

/// <summary>Creates lightweight low-poly visuals for world entities without external model dependencies.</summary>
public static class GeneratedAssetFactory
{
    public static Node3D CreateResource(string itemId) =>
        itemId switch
        {
            "logs" => Tree("#684321", "#2f6b36"),
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
            _ => Rodent(),
        };

    public static Node3D Tree(string trunkColor = "#654321", string leafColor = "#285b32")
    {
        var root = new Node3D { Name = "GeneratedTree" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.34f, Height = 2.8f, RadialSegments = 7 }, new Vector3(0, 1.4f, 0), trunkColor));
        root.AddChild(Part(new SphereMesh { Radius = 1.05f, Height = 2.1f, RadialSegments = 7, Rings = 4 }, new Vector3(0, 3.1f, 0), leafColor));
        root.AddChild(Part(new SphereMesh { Radius = 0.78f, Height = 1.5f, RadialSegments = 7, Rings = 4 }, new Vector3(0.62f, 3.2f, 0.1f), leafColor));
        root.AddChild(Part(new SphereMesh { Radius = 0.72f, Height = 1.45f, RadialSegments = 7, Rings = 4 }, new Vector3(-0.58f, 3.25f, -0.08f), leafColor));
        return root;
    }

    public static Node3D Ore(string stoneColor, string veinColor)
    {
        var root = new Node3D { Name = "GeneratedOre" };
        AddRock(root, new Vector3(0, 0.48f, 0), new Vector3(1.15f, 0.8f, 0.9f), stoneColor);
        AddRock(root, new Vector3(0.55f, 0.42f, 0.12f), new Vector3(0.62f, 0.58f, 0.55f), veinColor);
        AddRock(root, new Vector3(-0.44f, 0.34f, -0.1f), new Vector3(0.48f, 0.42f, 0.52f), veinColor);
        return root;
    }

    public static Node3D Herb(string flowerColor, string leafColor)
    {
        var root = new Node3D { Name = "GeneratedHerb" };
        for (var i = -2; i <= 2; i++)
        {
            root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.6f, 0.12f) }, new Vector3(i * 0.14f, 0.3f, (i % 2) * 0.08f), leafColor));
            root.AddChild(Part(new SphereMesh { Radius = 0.12f, Height = 0.24f, RadialSegments = 6, Rings = 3 }, new Vector3(i * 0.14f, 0.66f, (i % 2) * 0.08f), flowerColor));
        }
        return root;
    }

    public static Node3D FishingSpot()
    {
        var root = new Node3D { Name = "GeneratedFishingSpot" };
        root.AddChild(Part(new CylinderMesh { TopRadius = 0.72f, BottomRadius = 0.72f, Height = 0.04f, RadialSegments = 16 }, new Vector3(0, 0.03f, 0), "#3b8ead"));
        root.AddChild(Part(new TorusMesh { InnerRadius = 0.42f, OuterRadius = 0.58f, Rings = 12, RingSegments = 6 }, new Vector3(0, 0.08f, 0), "#b4d8df"));
        return root;
    }

    public static MeshInstance3D Rock(Vector3 position, float scale, string color)
    {
        var rock = Part(new SphereMesh { Radius = 0.8f, Height = 1.1f, RadialSegments = 6, Rings = 3 }, position + Vector3.Up * 0.5f, color);
        rock.Scale = new Vector3(scale * 1.4f, scale, scale);
        return rock;
    }

    private static Node3D Skeleton()
    {
        var root = new Node3D { Name = "GeneratedSkeleton" };
        root.AddChild(Part(new SphereMesh { Radius = 0.24f, Height = 0.48f, RadialSegments = 7, Rings = 4 }, new Vector3(0, 0.78f, 0), "#dad1b2"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.48f, 0.64f, 0.20f) }, new Vector3(0, 0.22f, 0), "#c3b990"));
        return root;
    }

    private static Node3D Marauder(string color)
    {
        var root = new Node3D { Name = "GeneratedMarauder" };
        root.AddChild(Part(new CapsuleMesh { Radius = 0.34f, Height = 1.35f, RadialSegments = 7, Rings = 3 }, Vector3.Zero, color));
        root.AddChild(Part(new SphereMesh { Radius = 0.27f, Height = 0.54f, RadialSegments = 7, Rings = 4 }, new Vector3(0, 0.85f, 0), "#7d9b55"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.14f, 0.85f, 0.12f) }, new Vector3(0.48f, 0.05f, 0), "#b6a06a"));
        return root;
    }

    private static Node3D Rodent()
    {
        var root = new Node3D { Name = "GeneratedRodent" };
        root.AddChild(Part(new SphereMesh { Radius = 0.42f, Height = 0.62f, RadialSegments = 7, Rings = 4 }, new Vector3(0, -0.28f, 0), "#765748"));
        root.AddChild(Part(new SphereMesh { Radius = 0.26f, Height = 0.4f, RadialSegments = 7, Rings = 4 }, new Vector3(0, -0.2f, -0.42f), "#876657"));
        return root;
    }

    private static void AddRock(Node3D root, Vector3 position, Vector3 scale, string color)
    {
        var rock = Part(new SphereMesh { Radius = 0.75f, Height = 1.0f, RadialSegments = 6, Rings = 3 }, position, color);
        rock.Scale = scale;
        root.AddChild(rock);
    }

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, string color)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position };
        part.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = new Color(color) });
        return part;
    }
}
