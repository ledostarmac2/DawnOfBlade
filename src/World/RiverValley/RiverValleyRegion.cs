using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.World.Grid;
using Godot;

namespace DawnOfBlade.World.RiverValley;

/// <summary>
/// Immutable starting-region index. Visual meshes consume this model, but pathing rules remain
/// server-facing tile metadata so client scenery can never become the authority.
/// </summary>
public sealed class RiverValleyRegion
{
    public const int Size = 128;
    public const float TileSizeMeters = 2.0f;

    public GridCoordinate RespawnTile { get; } = new(35, 35);
    public GridBounds CastleBounds { get; } = new(new GridCoordinate(20, 20), new GridCoordinate(50, 50));
    public GridBounds PastureBounds { get; } = new(new GridCoordinate(10, 55), new GridCoordinate(40, 90));
    public GridBounds GraveyardBounds { get; } = new(new GridCoordinate(10, 2), new GridCoordinate(30, 18));
    public GridBounds WoodlandsBounds { get; } = new(new GridCoordinate(65, 40), new GridCoordinate(120, 90));
    public GridBounds MineBounds { get; } = new(new GridCoordinate(80, 10), new GridCoordinate(110, 35));

    public IReadOnlyList<RegionAnchor> Anchors { get; }
    public IReadOnlyList<SpawnPoolDefinition> SpawnPools { get; }
    public IReadOnlySet<GridCoordinate> BlockedTiles => _blockedTiles;

    private readonly HashSet<GridCoordinate> _blockedTiles;

    public RiverValleyRegion()
    {
        Anchors = BuildAnchors();
        SpawnPools = BuildSpawnPools();
        _blockedTiles = BuildBlockedTiles(Anchors);
    }

    public bool IsInside(GridCoordinate tile) =>
        tile.X >= 0 && tile.X < Size && tile.Z >= 0 && tile.Z < Size;

    public bool IsWalkable(GridCoordinate tile) => IsInside(tile) && !_blockedTiles.Contains(tile);

    public Vector3 TileToWorld(GridCoordinate tile, float height = 0.0f) =>
        new((tile.X - RespawnTile.X) * TileSizeMeters, height, (tile.Z - RespawnTile.Z) * TileSizeMeters);

    public IEnumerable<RegionAnchor> AnchorsOfType(RegionAnchorType type) =>
        Anchors.Where(anchor => anchor.Type == type);

    private static IReadOnlyList<RegionAnchor> BuildAnchors()
    {
        var anchors = new List<RegionAnchor>
        {
            new("courtyard_spawn", RegionAnchorType.CharacterSpawn, new GridCoordinate(35, 35)),
            new("castle_hearth", RegionAnchorType.ProcessingStation, new GridCoordinate(45, 35), "cooking"),
            new("castle_spinning_wheel", RegionAnchorType.ProcessingStation, new GridCoordinate(25, 40), "crafting"),
            new("castle_level_2_stairs", RegionAnchorType.Staircase, new GridCoordinate(22, 22)),
            new("castle_level_3_stairs", RegionAnchorType.Staircase, new GridCoordinate(22, 48)),
            new("market_square", RegionAnchorType.Market, new GridCoordinate(35, 10)),
            new("toll_bridge", RegionAnchorType.Bridge, new GridCoordinate(57, 35)),
            new("gatekeeper", RegionAnchorType.Npc, new GridCoordinate(35, 20), "dialogue"),
            new("general_provisioner", RegionAnchorType.Npc, new GridCoordinate(32, 12), "shop"),
            new("combat_outfitter", RegionAnchorType.Npc, new GridCoordinate(38, 12), "shop"),
            new("copper_ore_01", RegionAnchorType.Resource, new GridCoordinate(85, 15), "mining", BlocksMovement: true),
            new("copper_ore_02", RegionAnchorType.Resource, new GridCoordinate(87, 14), "mining", BlocksMovement: true),
            new("copper_ore_03", RegionAnchorType.Resource, new GridCoordinate(89, 17), "mining", BlocksMovement: true),
            new("copper_ore_04", RegionAnchorType.Resource, new GridCoordinate(91, 13), "mining", BlocksMovement: true),
            new("copper_ore_05", RegionAnchorType.Resource, new GridCoordinate(93, 18), "mining", BlocksMovement: true),
            new("copper_ore_06", RegionAnchorType.Resource, new GridCoordinate(84, 20), "mining", BlocksMovement: true),
            new("tin_ore_01", RegionAnchorType.Resource, new GridCoordinate(95, 16), "mining", BlocksMovement: true),
            new("tin_ore_02", RegionAnchorType.Resource, new GridCoordinate(97, 15), "mining", BlocksMovement: true),
            new("tin_ore_03", RegionAnchorType.Resource, new GridCoordinate(99, 18), "mining", BlocksMovement: true),
            new("tin_ore_04", RegionAnchorType.Resource, new GridCoordinate(101, 14), "mining", BlocksMovement: true),
            new("tin_ore_05", RegionAnchorType.Resource, new GridCoordinate(103, 20), "mining", BlocksMovement: true),
            new("tin_ore_06", RegionAnchorType.Resource, new GridCoordinate(105, 17), "mining", BlocksMovement: true),
        };

        var trees = new[]
        {
            (70, 60), (74, 63), (78, 58), (82, 66), (87, 62), (91, 70), (96, 55),
            (100, 74), (105, 64), (109, 79), (114, 58), (116, 85), (72, 82), (88, 87),
        };
        for (var i = 0; i < trees.Length; i++)
        {
            anchors.Add(new RegionAnchor(
                $"softwood_tree_{i + 1:00}",
                RegionAnchorType.Resource,
                new GridCoordinate(trees[i].Item1, trees[i].Item2),
                "woodcutting",
                BlocksMovement: true));
        }

        return anchors;
    }

    private static IReadOnlyList<SpawnPoolDefinition> BuildSpawnPools() =>
        new[]
        {
            new SpawnPoolDefinition("pasture_poultry", "chicken", new GridBounds(new GridCoordinate(10, 55), new GridCoordinate(40, 90)), 12, 30, false),
            new SpawnPoolDefinition("graveyard_skeletons", "reanimated_skeleton", new GridBounds(new GridCoordinate(10, 2), new GridCoordinate(30, 18)), 8, 45, false),
            new SpawnPoolDefinition("woodland_marauders", "forest_marauder", new GridBounds(new GridCoordinate(65, 40), new GridCoordinate(120, 90)), 18, 40, true, 3),
            new SpawnPoolDefinition("mine_rodents", "cavern_rodent", new GridBounds(new GridCoordinate(80, 10), new GridCoordinate(110, 35)), 10, 35, true, 4),
        };

    private static HashSet<GridCoordinate> BuildBlockedTiles(IEnumerable<RegionAnchor> anchors)
    {
        var blocked = new HashSet<GridCoordinate>();

        // The river is impassable except for the reinforced bridge crossing.
        for (var z = 0; z < Size; z++)
        {
            for (var x = 55; x <= 59; x++)
            {
                if (z != 35)
                {
                    blocked.Add(new GridCoordinate(x, z));
                }
            }
        }

        // Castle perimeter walls with a southern gate opening.
        for (var x = 20; x <= 50; x++)
        {
            if (x != 35)
            {
                blocked.Add(new GridCoordinate(x, 20));
            }

            blocked.Add(new GridCoordinate(x, 50));
        }

        for (var z = 21; z < 50; z++)
        {
            blocked.Add(new GridCoordinate(20, z));
            blocked.Add(new GridCoordinate(50, z));
        }

        foreach (var anchor in anchors.Where(anchor => anchor.BlocksMovement))
        {
            blocked.Add(anchor.Coordinate);
        }

        return blocked;
    }
}
