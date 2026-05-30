using System;

namespace DawnOfBlade.World;

/// <summary>
/// Generates an original overworld of named, biome-tagged regions from a seed. Deterministic:
/// the same seed and dimensions always produce the same map, so the world is stable across runs.
/// </summary>
public sealed class WorldMapGenerator
{
    private static readonly string[] Biomes =
    {
        "Verdant Meadow", "Ashen Wastes", "Frostbound Tundra",
        "Emberwood", "Tidewater Coast", "Sunlit Downs",
    };

    private static readonly string[] NamePrefixes =
    {
        "North", "South", "East", "West", "High", "Low", "Old", "Far",
    };

    private static readonly string[] NameSuffixes =
    {
        "haven", "ford", "reach", "hollow", "crest", "mire", "vale", "watch",
    };

    public WorldMap Generate(int seed, int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var random = new Random(seed);
        var grid = new WorldRegion[width, height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var biome = Biomes[random.Next(Biomes.Length)];
                var name = NamePrefixes[random.Next(NamePrefixes.Length)] + NameSuffixes[random.Next(NameSuffixes.Length)];
                grid[x, y] = new WorldRegion(x, y, $"region_{x}_{y}", name, biome);
            }
        }

        return new WorldMap(width, height, grid);
    }
}
