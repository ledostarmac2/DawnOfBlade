using System.Collections.Generic;

namespace DawnOfBlade.World;

/// <summary>A single region tile in the world grid.</summary>
public sealed record WorldRegion(int X, int Y, string Id, string DisplayName, string Biome);

/// <summary>A grid of <see cref="WorldRegion"/> tiles forming the overworld.</summary>
public sealed class WorldMap
{
    private readonly WorldRegion[,] _regions;

    public WorldMap(int width, int height, WorldRegion[,] regions)
    {
        Width = width;
        Height = height;
        _regions = regions;
    }

    public int Width { get; }
    public int Height { get; }
    public int RegionCount => Width * Height;

    public WorldRegion Get(int x, int y) => _regions[x, y];

    public IEnumerable<WorldRegion> Regions()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                yield return _regions[x, y];
            }
        }
    }
}
