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
    public const float WorldSizeMeters = 3200.0f;
    public const float HalfWorldMeters = WorldSizeMeters * 0.5f;
    public const float VisualWorldScale = 0.88f;
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
        AddAtmosphere();
        AddLandscapeDistricts();
        AddRiver();
        AddBridge();
        AddCastlePerimeter();
        AddMarketSquare();
        AddRoadNetwork();
        AddRoadDetails();
        AddLandmarkBuildings();
        AddSettlementProps();
        AddIndexedResources();
        AddMonsterPools();
        AddBackgroundScenery();
        AddGroundDetail();
        AddContextualNpcs();
    }

    private void AddAtmosphere()
    {
        AddChild(new WorldEnvironment
        {
            Name = "WorldAtmosphere",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("#8aa9bd"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("#c9d3c0"),
                AmbientLightEnergy = 0.62f,
            },
        });

        AddChild(new DirectionalLight3D
        {
            Name = "SoftSkyFill",
            RotationDegrees = new Vector3(-28, 145, 0),
            LightColor = new Color("#9fb7cf"),
            LightEnergy = 0.22f,
            ShadowEnabled = false,
        });
    }

    private void AddRiver()
    {
        var x = TileToSceneWorld(new GridCoordinate(57, 35)).X;
        var riverSize = new Vector3(16 * VisualWorldScale, 0.08f, WorldSizeMeters);
        AddChild(Part(new BoxMesh { Size = riverSize }, new Vector3(x, 0.03f, 0), WaterMaterial()));

        var random = new Random(Seed + 91);
        AddRiverRipples(x, riverSize, random);
        for (var i = 0; i < 60; i++)
        {
            var z = ((float)random.NextDouble() * 2.0f - 1.0f) * HalfWorldMeters;
            var side = random.Next(2) == 0 ? -1.0f : 1.0f;
            var bankX = x + side * (riverSize.X * 0.5f + 0.9f + (float)random.NextDouble() * 1.4f);
            AddReedCluster(new Vector3(bankX, 0.12f, z), 0.75f + (float)random.NextDouble() * 0.55f, random);
        }

        var bridgeGap = RiverValleyRegion.TileSizeMeters * VisualWorldScale * 2.0f;
        var blockedLength = HalfWorldMeters - bridgeGap;
        AddCollisionBox(new Vector3(riverSize.X, 0.8f, blockedLength), new Vector3(x, 0.4f, (bridgeGap + HalfWorldMeters) * 0.5f));
        AddCollisionBox(new Vector3(riverSize.X, 0.8f, blockedLength), new Vector3(x, 0.4f, -(bridgeGap + HalfWorldMeters) * 0.5f));
    }

    private void AddRiverRipples(float riverX, Vector3 riverSize, Random random)
    {
        for (var i = 0; i < 36; i++)
        {
            var x = riverX + ((float)random.NextDouble() - 0.5f) * riverSize.X * 0.72f;
            var z = ((float)random.NextDouble() * 2.0f - 1.0f) * HalfWorldMeters;
            var length = 1.2f + (float)random.NextDouble() * 2.4f;
            var ripple = Part(
                new BoxMesh { Size = new Vector3(0.055f, 0.012f, length) },
                new Vector3(x, 0.095f, z),
                RippleMaterial(),
                new Vector3(0, ((float)random.NextDouble() - 0.5f) * 0.6f, 0));
            AddChild(ripple);
        }
    }

    private void AddBridge()
    {
        var position = TileToSceneWorld(new GridCoordinate(57, 35), 0.16f);
        var bridge = new Node3D { Name = "TimberBridge", Position = position };
        var deckSize = ScaleWorldSize(new Vector3(18, 0.25f, 3.2f));
        bridge.AddChild(Box(deckSize, Vector3.Zero, "#82552f"));

        for (var i = -5; i <= 5; i++)
        {
            bridge.AddChild(Box(new Vector3(0.18f, 0.08f, deckSize.Z + 0.18f), new Vector3(i * 1.35f, 0.16f, 0), "#6e4428"));
        }

        bridge.AddChild(Box(new Vector3(deckSize.X, 0.24f, 0.16f), new Vector3(0, 0.42f, deckSize.Z * 0.5f + 0.15f), "#4e3120"));
        bridge.AddChild(Box(new Vector3(deckSize.X, 0.24f, 0.16f), new Vector3(0, 0.42f, -deckSize.Z * 0.5f - 0.15f), "#4e3120"));
        AddChild(bridge);
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
            var towerSize = ScaleWorldSize(new Vector3(4.2f, 6.0f, 4.2f));
            var towerPosition = TileToSceneWorld(tile, 3.0f);
            AddChild(Box(towerSize, towerPosition, "#687174"));
            AddCollisionBox(towerSize, towerPosition);
        }
    }

    private void AddWall(GridCoordinate start, GridCoordinate end, string color)
    {
        var a = TileToSceneWorld(start, 1.6f);
        var b = TileToSceneWorld(end, 1.6f);
        var size = new Vector3(Mathf.Abs(b.X - a.X) + 2.0f, 3.2f, Mathf.Abs(b.Z - a.Z) + 2.0f);
        var position = (a + b) / 2;
        AddChild(Box(size, position, color));
        AddBattlements(position, size, color);
        AddCollisionBox(size, position);
    }

    private void AddMarketSquare()
    {
        var position = TileToSceneWorld(new GridCoordinate(35, 10), 0.04f);
        AddChild(Box(ScaleWorldSize(new Vector3(26, 0.08f, 14)), position, "#b69b70"));
    }

    private void AddLandscapeDistricts()
    {
        AddChild(Box(ScaleWorldSize(new Vector3(980, 0.05f, 760)), ScaleWorldPosition(new Vector3(-940, 0.02f, 860)), "#607f42"));
        AddChild(Box(ScaleWorldSize(new Vector3(900, 0.05f, 680)), ScaleWorldPosition(new Vector3(1020, 0.02f, 980)), "#88784a"));
        AddChild(Box(ScaleWorldSize(new Vector3(880, 0.05f, 680)), ScaleWorldPosition(new Vector3(-1100, 0.02f, -1040)), "#566f56"));
        AddChild(Box(ScaleWorldSize(new Vector3(940, 0.05f, 720)), ScaleWorldPosition(new Vector3(1080, 0.02f, -980)), "#8b7654"));
    }

    private void AddRoadNetwork()
    {
        var road = "#9b8661";
        AddChild(Box(ScaleWorldSize(new Vector3(22, 0.06f, 3000)), ScaleWorldPosition(new Vector3(-120, 0.07f, 0)), road));
        AddChild(Box(ScaleWorldSize(new Vector3(2700, 0.06f, 22)), ScaleWorldPosition(new Vector3(-160, 0.075f, 80)), road));
        AddChild(Box(ScaleWorldSize(new Vector3(1700, 0.06f, 18)), ScaleWorldPosition(new Vector3(-720, 0.08f, -940)), road));
        AddChild(Box(ScaleWorldSize(new Vector3(1450, 0.06f, 18)), ScaleWorldPosition(new Vector3(780, 0.08f, 980)), road));
    }

    private void AddRoadDetails()
    {
        var random = new Random(Seed + 211);
        AddRoadRuts(ScaleWorldPosition(new Vector3(-120, 0.13f, 0)), 2700, vertical: true);
        AddRoadRuts(ScaleWorldPosition(new Vector3(-160, 0.135f, 80)), 2400, vertical: false);

        for (var i = 0; i < 180; i++)
        {
            var onNorthSouth = random.NextDouble() < 0.52;
            var basePosition = onNorthSouth
                ? new Vector3(-120 + ((float)random.NextDouble() - 0.5f) * 18.0f, 0.16f, ((float)random.NextDouble() * 2.0f - 1.0f) * 1320.0f)
                : new Vector3(-160 + ((float)random.NextDouble() * 2.0f - 1.0f) * 1180.0f, 0.16f, 80 + ((float)random.NextDouble() - 0.5f) * 18.0f);
            AddRoadStone(ScaleWorldPosition(basePosition), 0.18f + (float)random.NextDouble() * 0.22f, random);
        }

        AddSignpost(ScaleWorldPosition(new Vector3(-130, 0, 84)), "Crossroads");
        AddSignpost(ScaleWorldPosition(new Vector3(70, 0, 78)), "Market");
        AddSignpost(ScaleWorldPosition(new Vector3(-125, 0, -115)), "Castle");
    }

    private void AddRoadRuts(Vector3 center, float length, bool vertical)
    {
        var ruts = new Node3D { Name = "RoadRuts", Position = center };
        var longSize = vertical ? new Vector3(0.24f, 0.025f, length * VisualWorldScale) : new Vector3(length * VisualWorldScale, 0.025f, 0.24f);
        var offsetA = vertical ? new Vector3(-2.4f, 0, 0) : new Vector3(0, 0, -2.4f);
        var offsetB = vertical ? new Vector3(2.4f, 0, 0) : new Vector3(0, 0, 2.4f);
        ruts.AddChild(Box(longSize, offsetA, "#74644a"));
        ruts.AddChild(Box(longSize, offsetB, "#74644a"));
        AddChild(ruts);
    }

    private void AddRoadStone(Vector3 position, float scale, Random random)
    {
        var stone = Part(new SphereMesh { Radius = 0.32f, Height = 0.18f, RadialSegments = 6, Rings = 3 }, position, random.Next(2) == 0 ? "#aa9b80" : "#837762");
        stone.Scale = new Vector3(scale * 1.25f, scale * 0.34f, scale * 0.85f);
        stone.Rotation = new Vector3(0, (float)random.NextDouble() * Mathf.Tau, 0);
        AddChild(stone);
    }

    private void AddSignpost(Vector3 position, string name)
    {
        var sign = new Node3D { Name = $"Signpost_{name.Replace(' ', '_')}", Position = position };
        sign.AddChild(Part(new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.08f, Height = 1.65f, RadialSegments = 6 }, new Vector3(0, 0.82f, 0), "#4a3122"));
        sign.AddChild(Box(new Vector3(1.35f, 0.36f, 0.12f), new Vector3(0.4f, 1.42f, 0), "#7b5634"));
        sign.AddChild(Box(new Vector3(0.2f, 0.04f, 0.14f), new Vector3(0.95f, 1.42f, 0), "#d7bf74"));
        AddChild(sign);
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

    private void AddSettlementProps()
    {
        AddMarketStall(ScaleWorldPosition(new Vector3(164, 0, 82)), "#b84536");
        AddMarketStall(ScaleWorldPosition(new Vector3(196, 0, 106)), "#d2a646");
        AddMarketStall(ScaleWorldPosition(new Vector3(172, 0, 122)), "#3f6d9d");

        AddCrateStack(ScaleWorldPosition(new Vector3(138, 0, 116)));
        AddCrateStack(ScaleWorldPosition(new Vector3(-116, 0, -148)));
        AddBarrelCluster(ScaleWorldPosition(new Vector3(-150, 0, -185)));
        AddBarrelCluster(ScaleWorldPosition(new Vector3(214, 0, 76)));

        foreach (var point in new[]
                 {
                     new Vector3(-44, 0, 34), new Vector3(44, 0, 34),
                     new Vector3(-46, 0, -34), new Vector3(46, 0, -34),
                     new Vector3(135, 0, 64), new Vector3(220, 0, 128),
                 })
        {
            AddLampPost(ScaleWorldPosition(point));
        }

        AddFenceLine(ScaleWorldPosition(new Vector3(-760, 0, 395)), 9, horizontal: true);
        AddFenceLine(ScaleWorldPosition(new Vector3(-760, 0, 505)), 9, horizontal: true);
    }

    private void AddMarketStall(Vector3 position, string awningColor)
    {
        var stall = new Node3D { Name = "MarketStall", Position = position };
        stall.AddChild(Box(new Vector3(4.4f, 0.28f, 2.6f), new Vector3(0, 0.72f, 0), "#6b4a2f"));
        stall.AddChild(Box(new Vector3(4.9f, 0.18f, 3.0f), new Vector3(0, 2.1f, 0), awningColor));
        stall.AddChild(Box(new Vector3(0.16f, 1.6f, 0.16f), new Vector3(-1.9f, 1.3f, -1.05f), "#4e3424"));
        stall.AddChild(Box(new Vector3(0.16f, 1.6f, 0.16f), new Vector3(1.9f, 1.3f, -1.05f), "#4e3424"));
        stall.AddChild(Box(new Vector3(0.16f, 1.6f, 0.16f), new Vector3(-1.9f, 1.3f, 1.05f), "#4e3424"));
        stall.AddChild(Box(new Vector3(0.16f, 1.6f, 0.16f), new Vector3(1.9f, 1.3f, 1.05f), "#4e3424"));
        stall.AddChild(Part(new SphereMesh { Radius = 0.22f, Height = 0.24f, RadialSegments = 6, Rings = 3 }, new Vector3(-1.0f, 0.98f, 0.15f), "#c85f3d"));
        stall.AddChild(Part(new SphereMesh { Radius = 0.2f, Height = 0.22f, RadialSegments = 6, Rings = 3 }, new Vector3(-0.55f, 0.98f, -0.2f), "#d6b34c"));
        stall.AddChild(Part(new SphereMesh { Radius = 0.18f, Height = 0.2f, RadialSegments = 6, Rings = 3 }, new Vector3(0.15f, 0.98f, 0.18f), "#7ba34b"));
        AddChild(stall);
    }

    private void AddCrateStack(Vector3 position)
    {
        var crates = new Node3D { Name = "CrateStack", Position = position };
        crates.AddChild(Box(new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0, 0.45f, 0), "#7a5636"));
        crates.AddChild(Box(new Vector3(0.85f, 0.85f, 0.85f), new Vector3(0.78f, 0.43f, 0.18f), "#6e4a2f"));
        crates.AddChild(Box(new Vector3(0.72f, 0.72f, 0.72f), new Vector3(0.32f, 1.26f, -0.16f), "#8a6140"));
        AddChild(crates);
    }

    private void AddBarrelCluster(Vector3 position)
    {
        var barrels = new Node3D { Name = "BarrelCluster", Position = position };
        barrels.AddChild(Part(new CylinderMesh { TopRadius = 0.38f, BottomRadius = 0.42f, Height = 0.9f, RadialSegments = 10 }, new Vector3(0, 0.45f, 0), "#6c452d"));
        barrels.AddChild(Part(new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0.36f, Height = 0.78f, RadialSegments = 10 }, new Vector3(0.58f, 0.39f, 0.18f), "#744d32"));
        barrels.AddChild(Box(new Vector3(0.9f, 0.08f, 0.08f), new Vector3(0, 0.72f, 0), "#2c2722"));
        AddChild(barrels);
    }

    private void AddLampPost(Vector3 position)
    {
        var lamp = new Node3D { Name = "LampPost", Position = position };
        lamp.AddChild(Part(new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.1f, Height = 2.2f, RadialSegments = 7 }, new Vector3(0, 1.1f, 0), "#3b2a1d"));
        lamp.AddChild(Box(new Vector3(0.52f, 0.38f, 0.52f), new Vector3(0, 2.3f, 0), "#d6a84a"));
        lamp.AddChild(new OmniLight3D
        {
            Position = new Vector3(0, 2.32f, 0),
            LightColor = new Color("#ffd98a"),
            LightEnergy = 0.18f,
            OmniRange = 7.0f,
        });
        AddChild(lamp);
    }

    private void AddFenceLine(Vector3 start, int posts, bool horizontal)
    {
        var fence = new Node3D { Name = "FenceLine", Position = start };
        for (var i = 0; i < posts; i++)
        {
            var offset = horizontal ? new Vector3(i * 5.0f, 0, 0) : new Vector3(0, 0, i * 5.0f);
            fence.AddChild(Box(new Vector3(0.22f, 1.15f, 0.22f), offset + new Vector3(0, 0.58f, 0), "#5a3a24"));
        }

        var railSize = horizontal ? new Vector3(posts * 5.0f, 0.16f, 0.16f) : new Vector3(0.16f, 0.16f, posts * 5.0f);
        var railCenter = horizontal ? new Vector3((posts - 1) * 2.5f, 0.78f, 0) : new Vector3(0, 0.78f, (posts - 1) * 2.5f);
        fence.AddChild(Box(railSize, railCenter, "#6c452d"));
        AddChild(fence);
    }

    private void AddBuilding(string name, Vector3 position, Vector2 footprint, float height, string walls, string roof)
    {
        var building = new WorldBuilding(name, footprint * VisualWorldScale, height, new Color(walls), new Color(roof))
        {
            Position = ScaleWorldPosition(position),
        };
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
            var position = TileToSceneWorld(anchor.Coordinate);
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

    private void AddGroundDetail()
    {
        var random = new Random(Seed + 137);
        for (var i = 0; i < 220; i++)
        {
            var position = NextWorldPosition(random);
            if (position.Length() < 36)
            {
                continue;
            }

            if (random.NextDouble() < 0.72)
            {
                AddGrassTuft(position, 0.65f + (float)random.NextDouble() * 0.7f, random);
            }
            else
            {
                AddPebbleCluster(position, 0.35f + (float)random.NextDouble() * 0.75f, random);
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
            position = ScaleWorldPosition(position);
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
            Position = TileToSceneWorld(tile, 0.9f),
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

    private Vector3 TileToSceneWorld(GridCoordinate tile, float height = 0.0f) =>
        ScaleWorldPosition(_region.TileToWorld(tile, height));

    private static Vector3 ScaleWorldPosition(Vector3 position) =>
        new(position.X * VisualWorldScale, position.Y, position.Z * VisualWorldScale);

    private static Vector3 ScaleWorldSize(Vector3 size) =>
        new(size.X * VisualWorldScale, size.Y, size.Z * VisualWorldScale);

    private static bool NearCourtyard(GridCoordinate tile) =>
        tile.ChebyshevDistanceTo(new GridCoordinate(35, 35)) < 18;

    private void AddTree(Vector3 position, float scale)
    {
        var root = GeneratedAssetFactory.Tree();
        root.Position = position;
        root.Scale = Vector3.One * scale;
        AddChild(root);
    }

    private void AddGrassTuft(Vector3 position, float scale, Random random)
    {
        var root = new Node3D { Name = "GrassTuft", Position = position };
        var bladeCount = 3 + random.Next(4);
        for (var i = 0; i < bladeCount; i++)
        {
            var angle = (float)random.NextDouble() * Mathf.Tau;
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ((float)random.NextDouble() * 0.28f);
            var height = (0.35f + (float)random.NextDouble() * 0.35f) * scale;
            var blade = Part(new BoxMesh { Size = new Vector3(0.055f, height, 0.035f) }, offset + Vector3.Up * (height * 0.5f), "#4f7538");
            blade.Rotation = new Vector3((float)random.NextDouble() * 0.26f, angle, (float)random.NextDouble() * 0.22f);
            root.AddChild(blade);
        }

        AddChild(root);
    }

    private void AddReedCluster(Vector3 position, float scale, Random random)
    {
        var root = new Node3D { Name = "RiverReeds", Position = position };
        for (var i = 0; i < 4; i++)
        {
            var angle = (float)random.NextDouble() * Mathf.Tau;
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ((float)random.NextDouble() * 0.4f);
            var height = (0.75f + (float)random.NextDouble() * 0.55f) * scale;
            var stem = Part(new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.035f, Height = height, RadialSegments = 5 }, offset + Vector3.Up * (height * 0.5f), "#667f38");
            stem.Rotation = new Vector3(((float)random.NextDouble() - 0.5f) * 0.18f, angle, ((float)random.NextDouble() - 0.5f) * 0.18f);
            root.AddChild(stem);
            root.AddChild(Part(new SphereMesh { Radius = 0.055f, Height = 0.18f, RadialSegments = 5, Rings = 3 }, offset + Vector3.Up * (height + 0.08f), "#7a5f32"));
        }

        AddChild(root);
    }

    private void AddPebbleCluster(Vector3 position, float scale, Random random)
    {
        var root = new Node3D { Name = "PebbleCluster", Position = position };
        for (var i = 0; i < 3 + random.Next(3); i++)
        {
            var offset = new Vector3(((float)random.NextDouble() - 0.5f) * 1.2f, 0.04f, ((float)random.NextDouble() - 0.5f) * 1.2f);
            var pebble = Part(new SphereMesh { Radius = 0.18f, Height = 0.24f, RadialSegments = 6, Rings = 3 }, offset, random.Next(2) == 0 ? "#6f716a" : "#595e58");
            pebble.Scale = new Vector3(scale * (0.75f + (float)random.NextDouble() * 0.6f), scale * 0.42f, scale * (0.65f + (float)random.NextDouble() * 0.5f));
            root.AddChild(pebble);
        }

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
        AddTreeSiteProps(position);
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
        AddMineSiteProps(position, isTin);
        AddChild(node);
    }

    private void AddTreeSiteProps(Vector3 position)
    {
        var props = new Node3D { Name = "TreeSiteProps", Position = position };
        props.AddChild(Part(new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.26f, Height = 0.52f, RadialSegments = 8 }, new Vector3(-0.85f, 0.26f, 0.4f), "#5b3822", new Vector3(Mathf.Pi / 2, 0.3f, 0)));
        props.AddChild(Part(new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.24f, Height = 0.62f, RadialSegments = 8 }, new Vector3(-1.18f, 0.24f, 0.08f), "#684321", new Vector3(Mathf.Pi / 2, -0.25f, 0)));
        props.AddChild(Part(new SphereMesh { Radius = 0.16f, Height = 0.12f, RadialSegments = 6, Rings = 3 }, new Vector3(0.64f, 0.08f, -0.58f), "#d8c08a"));
        props.AddChild(Part(new SphereMesh { Radius = 0.12f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, new Vector3(0.88f, 0.07f, -0.44f), "#c99a6d"));
        AddChild(props);
    }

    private void AddMineSiteProps(Vector3 position, bool isTin)
    {
        var props = new Node3D { Name = "MineSiteProps", Position = position };
        var glint = isTin ? "#c4d0ca" : "#d4845e";
        props.AddChild(Part(new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.045f, Height = 1.2f, RadialSegments = 6 }, new Vector3(-0.95f, 0.48f, -0.34f), "#6e4a2f", new Vector3(0.76f, 0, -0.34f)));
        props.AddChild(Box(new Vector3(0.54f, 0.12f, 0.2f), new Vector3(-1.3f, 0.9f, -0.52f), glint));
        props.AddChild(Part(new SphereMesh { Radius = 0.18f, Height = 0.16f, RadialSegments = 6, Rings = 3 }, new Vector3(0.85f, 0.09f, 0.52f), "#4f5350"));
        props.AddChild(Part(new SphereMesh { Radius = 0.13f, Height = 0.12f, RadialSegments = 6, Rings = 3 }, new Vector3(1.06f, 0.07f, 0.26f), glint));
        AddChild(props);
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

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, string color, Vector3 rotation = default)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation };
        part.SetSurfaceOverrideMaterial(0, GroundedMaterial(color));
        return part;
    }

    private static MeshInstance3D Part(PrimitiveMesh mesh, Vector3 position, Material material, Vector3 rotation = default)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation };
        part.SetSurfaceOverrideMaterial(0, material);
        return part;
    }

    private static MeshInstance3D Box(Vector3 size, Vector3 position, string color) =>
        Part(new BoxMesh { Size = size }, position, color);

    private void AddBattlements(Vector3 wallPosition, Vector3 wallSize, string color)
    {
        var horizontal = wallSize.X >= wallSize.Z;
        var length = horizontal ? wallSize.X : wallSize.Z;
        var count = Mathf.Max(2, Mathf.FloorToInt(length / 4.4f));
        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 0.0f : i / (float)(count - 1) - 0.5f;
            var offset = horizontal
                ? new Vector3(t * (length - 1.5f), wallSize.Y * 0.5f + 0.42f, 0)
                : new Vector3(0, wallSize.Y * 0.5f + 0.42f, t * (length - 1.5f));
            AddChild(Box(new Vector3(horizontal ? 1.35f : wallSize.X, 0.84f, horizontal ? wallSize.Z : 1.35f), wallPosition + offset, color));
        }
    }

    private void AddCollisionBox(Vector3 size, Vector3 position)
    {
        var body = new StaticBody3D
        {
            Name = "GeneratedCollision",
            Position = position,
        };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }

    private static StandardMaterial3D GroundedMaterial(string color) => new()
    {
        AlbedoColor = new Color(color),
        Roughness = 0.88f,
        Metallic = 0.0f,
        SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx,
    };

    private static StandardMaterial3D WaterMaterial() => new()
    {
        AlbedoColor = new Color(0.14f, 0.48f, 0.62f, 0.74f),
        Roughness = 0.18f,
        Metallic = 0.0f,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
    };

    private static StandardMaterial3D RippleMaterial() => new()
    {
        AlbedoColor = new Color(0.78f, 0.92f, 0.96f, 0.42f),
        Roughness = 0.24f,
        Metallic = 0.0f,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
    };
}
