using System.Linq;
using DawnOfBlade.Characters;
using DawnOfBlade.World;
using Xunit;

namespace DawnOfBlade.Tests;

public class CharacterWorldTests
{
    [Fact]
    public void NpcRandomizer_IsDeterministicForSeed()
    {
        var randomizer = new NpcRandomizer();

        var a = randomizer.Generate(42);
        var b = randomizer.Generate(42);

        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Role, b.Role);
        Assert.Equal(a.Appearance.ShirtColor, b.Appearance.ShirtColor);
        Assert.Equal(a.Appearance.SkinTone, b.Appearance.SkinTone);
    }

    [Fact]
    public void NpcRandomizer_DrawsFromValidOptions()
    {
        var npc = new NpcRandomizer().Generate(123);

        Assert.Contains(npc.Appearance.SkinTone, AppearanceOptions.SkinTones);
        Assert.Contains(npc.Appearance.ShirtColor, AppearanceOptions.ShirtColors);
        Assert.InRange(npc.Appearance.HairStyle, 0, AppearanceOptions.HairStyleCount - 1);
    }

    [Fact]
    public void WorldMapGenerator_ProducesFullGrid()
    {
        var map = new WorldMapGenerator().Generate(7, 4, 3);

        Assert.Equal(12, map.RegionCount);
        Assert.Equal(12, map.Regions().Count());
    }

    [Fact]
    public void WorldMapGenerator_IsDeterministicForSeed()
    {
        var first = new WorldMapGenerator().Generate(7, 4, 3);
        var second = new WorldMapGenerator().Generate(7, 4, 3);

        Assert.Equal(first.Get(2, 1).Biome, second.Get(2, 1).Biome);
        Assert.Equal(first.Get(2, 1).DisplayName, second.Get(2, 1).DisplayName);
    }
}
