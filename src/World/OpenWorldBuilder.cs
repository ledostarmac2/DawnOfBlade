using System;
using System.Linq;
using Godot;
using DawnOfBlade.Characters;
using DawnOfBlade.Interaction;
using DawnOfBlade.World.Grid;
using DawnOfBlade.World.RiverValley;

namespace DawnOfBlade.World;

/// <summary>
/// Builds low-poly client scenery from the authoritative River Valley tile index. These meshes are
/// presentation only; collision and pathing decisions come from <see cref="RiverValleyRegion"/>.
/// </summary>
public partial class OpenWorldBuilder : Node3D
{
    public const float WorldSizeMeters = 3600.0f;
    public const float HalfWorldMeters = WorldSizeMeters * 0.5f;
    public const int LandmarkBuildingCount = 10;
    public const int ContextualNpcCount = 60;

    [Export] public int Seed { get; set; } = 2026;
    [Export] public int BackgroundTreeCount { get; set; } = 360;
    [Export] public int BackgroundRockCount { get; set; } = 180;
    [Export] public int HillCount { get; set; } = 84;

    private RiverValleyRegion _region = new();
    private Node3D? _player;

    public override void _Ready()
    {
        _player = GetParent()?.GetNodeOrNull<Node3D>("Player");
        AddLandscapeDistricts();
        AddRiver();
        AddBridge();
        AddCastlePerimeter();
        AddMarketSquare();
        AddRoadNetwork();
        AddLandmarkBuildings();
        AddIndexedResources();
        AddMonsterPools();
        AddBackgroundScenery();
        AddContextualNpcs();
    }

    private void AddRiver()
    {
        var x = _region.TileToWorld(new GridCoordinate(57, 35)).X;
        AddChild(Box(new Vector3(16, 0.08f, WorldSizeMeters), new Vector3(x, 0.03f, 0), "#287b9f"));
    }

    private void AddBridge()
    {
        var position = _region.TileToWorld(new GridCoordinate(57, 35), 0.16f);
        AddChild(Box(new Vector3(18, 0.25f, 3.2f), position, "#82552f"));
    }

    private void AddCastlePerimeter()
    {
        var stone = "#8a9294";
        AddWall(new GridCoordinate(20, 20), new GridCoordinate(34, 20), stone);
        AddWall(new GridCoordinate(36, 20), new GridCoordinate(50, 20), stone);
        AddWall(new GridCoordinate(20, 50), new GridCoordinate(50, 50), stone);
        AddWall(new GridCoordinate(20, 21), new GridCoordinate(20, 49), stone);
        AddWall(new GridCoordinate(50, 21), new GridCoordinate(50, 49), stone);

        foreach (var tile in new[]
                 {
                     new GridCoordinate(20, 20), new GridCoordinate(50, 20),
                     new GridCoordinate(20, 50), new GridCoordinate(50, 50),
                 })
        {
            AddChild(Box(new Vector3(4.2f, 6.0f, 4.2f), _region.TileToWorld(tile, 3.0f), "#687174"));
        }
    }

    private void AddWall(GridCoordinate start, GridCoordinate end, string color)
    {
        var a = _region.TileToWorld(start, 1.6f);
        var b = _region.TileToWorld(end, 1.6f);
        var size = new Vector3(Mathf.Abs(b.X - a.X) + 2.0f, 3.2f, Mathf.Abs(b.Z - a.Z) + 2.0f);
        AddChild(Box(size, (a + b) / 2, color));
    }

    private void AddMarketSquare()
    {
        var position = _region.TileToWorld(new GridCoordinate(35, 10), 0.04f);
        AddChild(Box(new Vector3(26, 0.08f, 14), position, "#b69b70"));
    }

    private void AddLandscapeDistricts()
    {
        AddChild(Box(new Vector3(980, 0.05f, 760), new Vector3(-940, 0.02f, 860), "#607f42"));
        AddChild(Box(new Vector3(900, 0.05f, 680), new Vector3(1020, 0.02f, 980), "#88784a"));
        AddChild(Box(new Vector3(880, 0.05f, 680), new Vector3(-1100, 0.02f, -1040), "#566f56"));
        AddChild(Box(new Vector3(940, 0.05f, 720), new Vector3(1080, 0.02f, -980), "#8b7654"));
    }

    private void AddRoadNetwork()
    {
        var road = "#9b8661";
        AddChild(Box(new Vector3(22, 0.06f, 3100), new Vector3(-120, 0.07f, 0), road));
        AddChild(Box(new Vector3(2800, 0.06f, 22), new Vector3(-160, 0.075f, 80), road));
        AddChild(Box(new Vector3(1700, 0.06f, 18), new Vector3(-720, 0.08f, -940), road));
        AddChild(Box(new Vector3(1450, 0.06f, 18), new Vector3(780, 0.08f, 980), road));
    }

    private void AddLandmarkBuildings()
    {
        AddBuilding("Citadel Great Hall", new Vector3(0, 0, 0), new Vector2(38, 32), 9.0f, "#9da4a5", "#4d5960");
        AddBuilding("River Valley Bank", new Vector3(-130, 0, -165), new Vector2(30, 24), 7.0f, "#ad9972", "#593f35");
        AddBuilding("Crafting Guild", new Vector3(-210, 0, 155), new Vector2(34, 26), 7.5f, "#8e785e", "#536049");
        AddBuilding("Market Hall", new Vector3(180, 0, 95), new Vector2(42, 28), 8.0f, "#a89069", "#664633");
        AddBuilding("Frontier Inn", new Vector3(520, 0, 160), new Vector2(32, 24), 7.0f, "#907251", "#543b2c");
        AddBuilding("Western Farmstead", new Vector3(-720, 0, 450), new Vector2(40, 30), 7.0f, "#997a56", "#5b402d");
        AddBuilding("Highland Monastery", new Vector3(-1180, 0, 1080), new Vector2(68, 46), 13.0f, "#9b9a91", "#495765");
        AddBuilding("Badlands Fortress", new Vector3(1260, 0, 980), new Vector2(88, 64), 16.0f, "#857661", "#4d433c");
        AddBuilding("Mirewatch Lodge", new Vector3(-1260, 0, -1110), new Vector2(52, 38), 10.0f, "#657460", "#3e493f");
        AddBuilding("Coastal Trade House", new Vector3(1180, 0, -1080), new Vector2(72, 44), 12.0f, "#a38b6c", "#4d6170");
    }

    private void AddBuilding(string name, Vector3 position, Vector2 footprint, float height, string walls, string roof)
    {
        var building = new WorldBuilding(name, footprint, height, new Color(walls), new Color(roof)) { Position = position };
        if (_player is not null)
        {
            building.Follow(_player);
        }
        AddChild(building);
    }

    private void AddIndexedResources()
    {
        foreach (var anchor in _region.AnchorsOfType(RegionAnchorType.Resource))
        {
            var position = _region.TileToWorld(anchor.Coordinate);
            if (anchor.InteractionType == "woodcutting")
            {
                AddInteractiveTree(anchor, position);
            }
            else
            {
                AddInteractiveRock(anchor, position);
            }
        }
    }

    private void AddBackgroundScenery()
    {
        var random = new Random(Seed);
        for (var i = 0; i < HillCount; i++)
        {
            var position = NextWorldPosition(random);
            var hill = Part(new SphereMesh { Radius = 10.0f, Height = 13.0f, RadialSegments = 7, Rings = 4 }, position + Vector3.Up * 2.4f, "#526d43");
            hill.Scale = new Vector3(1.6f + (float)random.NextDouble() * 2.5f, 0.45f + (float)random.NextDouble() * 0.8f, 1.4f + (float)random.NextDouble() * 2.0f);
            AddChild(hill);
        }

        for (var i = 0; i < BackgroundTreeCount; i++)
        {
            var position = NextWorldPosition(random);
            if (position.Length() > 90)
            {
                AddTree(position, 0.7f + (float)random.NextDouble() * 1.15f);
            }
        }

        for (var i = 0; i < BackgroundRockCount; i++)
        {
            var position = NextWorldPosition(random);
            if (position.Length() > 76)
            {
                AddRock(position, 0.35f + (float)random.NextDouble() * 0.8f);
            }
        }
    }

    private void AddContextualNpcs()
    {
        var random = new Random(Seed + 41);
        AddNpcCluster(random, "citadel", Vector3.Zero, new Vector2(120, 110), 12);
        AddNpcCluster(random, "market", new Vector3(180, 0, 95), new Vector2(170, 120), 12);
        AddNpcCluster(random, "farm", new Vector3(-720, 0, 450), new Vector2(220, 170), 8);
        AddNpcCluster(random, "monastery", new Vector3(-1180, 0, 1080), new Vector2(190, 150), 6);
        AddNpcCluster(random, "fortress", new Vector3(1260, 0, 980), new Vector2(210, 170), 8);
        AddNpcCluster(random, "lodge", new Vector3(-1260, 0, -1110), new Vector2(180, 140), 6);
        AddNpcCluster(random, "port", new Vector3(1180, 0, -1080), new Vector2(230, 170), 8);
    }

    private void AddNpcCluster(Random random, string district, Vector3 center, Vector2 spread, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var position = center + new Vector3(
                ((float)random.NextDouble() - 0.5f) * spread.X,
                0.9f,
                ((float)random.NextDouble() - 0.5f) * spread.Y);
            var npc = new PrototypeNpc
            {
                Name = $"{district}_npc_{i + 1:00}",
                Position = position,
                Seed = Seed + StableHash(district) + i,
            };
            npc.AddChild(new HumanoidVisual { Name = "Humanoid", Position = new Vector3(0, -0.9f, 0) });
            npc.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.35f, Height = 1.8f } });
            AddChild(npc);
        }
    }

    private void AddMonsterPools()
    {
        var random = new Random(Seed + 17);
        foreach (var pool in _region.SpawnPools.Where(pool => pool.EntityId != "chicken"))
        {
            var visiblePrototypeCount = Math.Min(pool.MaximumActive, 4);
            for (var i = 0; i < visiblePrototypeCount; i++)
            {
                AddHostile(pool, i, NextTile(random, pool.Bounds));
            }
        }
    }

    private void AddHostile(SpawnPoolDefinition pool, int index, GridCoordinate tile)
    {
        var stats = pool.EntityId switch
        {
            "reanimated_skeleton" => (name: "Reanimated Skeleton", hp: 8, attack: 2, strength: 2, defense: 1, color: "#d8d0b2"),
            "forest_marauder" => (name: "Forest Marauder", hp: 9, attack: 3, strength: 3, defense: 2, color: "#577238"),
            _ => (name: "Cavern Rodent", hp: 7, attack: 3, strength: 2, defense: 3, color: "#765748"),
        };

        var hostile = new HostileActor
        {
            Name = $"{pool.Id}_{index + 1:00}",
            DisplayName = stats.name,
            Position = _region.TileToWorld(tile, 0.9f),
            MaxHitpoints = stats.hp,
            AttackLevel = stats.attack,
            StrengthLevel = stats.strength,
            DefenseLevel = stats.defense,
            LootItemId = "coins",
            LootQuantity = pool.EntityId == "forest_marauder" ? 5 : 2,
        };
        hostile.AddChild(GeneratedAssetFactory.CreateHostile(pool.EntityId, stats.color));
        hostile.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.36f, Height = 1.8f } });
        AddChild(hostile);
    }

    private static GridCoordinate NextTile(Random random) =>
        new(random.Next(RiverValleyRegion.Size), random.Next(RiverValleyRegion.Size));

    private static Vector3 NextWorldPosition(Random random) =>
        new(((float)random.NextDouble() * 2.0f - 1.0f) * HalfWorldMeters, 0, ((float)random.NextDouble() * 2.0f - 1.0f) * HalfWorldMeters);

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in value)
            {
                hash = hash * 31 + character;
            }

            return hash;
        }
    }

    private static GridCoordinate NextTile(Random random, GridBounds bounds) =>
        new(random.Next(bounds.Minimum.X, bounds.Maximum.X + 1), random.Next(bounds.Minimum.Z, bounds.Maximum.Z + 1));

    private static bool NearCourtyard(GridCoordinate tile) =>
        tile.ChebyshevDistanceTo(new GridCoordinate(35, 35)) < 18;

    private void AddTree(Vector3 position, float scale)
    {
        var root = GeneratedAssetFactory.Tree();
        root.Position = position;
        root.Scale = Vector3.One * scale;
        AddChild(root);
    }

    private void AddInteractiveTree(RegionAnchor anchor, Vector3 position)
    {
        var node = Resource(anchor, position, "Softwood Tree", "logs", "woodcutting", 18);
        node.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 1.4f, 0),
            Shape = new CylinderShape3D { Radius = 0.55f, Height = 2.8f },
        });
        AddChild(node);
    }

    private void AddRock(Vector3 position, float scale)
    {
        AddChild(GeneratedAssetFactory.Rock(position, scale, "#66706f"));
    }

    private void AddInteractiveRock(RegionAnchor anchor, Vector3 position)
    {
        var isTin = anchor.Id.StartsWith("tin_ore_");
        var node = Resource(anchor, position, isTin ? "Tin Ore Vein" : "Copper Ore Vein", isTin ? "tin_ore" : "copper_ore", "mining", 18);
        node.AddChild(new CollisionShape3D
        {
            Position = Vector3.Up * 0.5f,
            Shape = new BoxShape3D { Size = new Vector3(1.5f, 1.1f, 1.3f) },
        });
        AddChild(node);
    }

    private static ResourceNode Resource(
        RegionAnchor anchor,
        Vector3 position,
        string displayName,
        string itemId,
        string skillId,
        int experience) =>
        new()
        {
            Name = anchor.Id,
            Position = position,
            DisplayName = displayName,
            ItemId = itemId,
            SkillId = skillId,
            Experience = experience,
        };

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, string color)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position };
        part.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = new Color(color) });
        return part;
    }

    private static MeshInstance3D Box(Vector3 size, Vector3 position, string color) =>
        Part(new BoxMesh { Size = size }, position, color);
}
