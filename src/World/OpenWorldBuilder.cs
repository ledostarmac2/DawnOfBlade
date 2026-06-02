using System;
using System.Linq;
using Godot;
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
    [Export] public int Seed { get; set; } = 2026;
    [Export] public int BackgroundTreeCount { get; set; } = 56;
    [Export] public int BackgroundRockCount { get; set; } = 32;

    private RiverValleyRegion _region = new();

    public override void _Ready()
    {
        AddRiver();
        AddBridge();
        AddCastlePerimeter();
        AddMarketSquare();
        AddIndexedResources();
        AddMonsterPools();
        AddBackgroundScenery();
    }

    private void AddRiver()
    {
        var south = _region.TileToWorld(new GridCoordinate(57, 0));
        var north = _region.TileToWorld(new GridCoordinate(57, RiverValleyRegion.Size - 1));
        var length = north.Z - south.Z + RiverValleyRegion.TileSizeMeters;
        AddChild(Box(new Vector3(10, 0.08f, length), new Vector3(south.X, 0.03f, (south.Z + north.Z) / 2), "#287b9f"));
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
        for (var i = 0; i < BackgroundTreeCount; i++)
        {
            var tile = NextTile(random);
            if (_region.IsWalkable(tile) && !NearCourtyard(tile))
            {
                AddTree(_region.TileToWorld(tile), 0.7f + (float)random.NextDouble() * 0.75f);
            }
        }

        for (var i = 0; i < BackgroundRockCount; i++)
        {
            var tile = NextTile(random);
            if (_region.IsWalkable(tile) && !NearCourtyard(tile))
            {
                AddRock(_region.TileToWorld(tile), 0.35f + (float)random.NextDouble() * 0.55f);
            }
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
