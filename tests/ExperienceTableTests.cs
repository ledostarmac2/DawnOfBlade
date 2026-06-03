using DawnOfBlade.Engine.Progression;
using Xunit;

namespace DawnOfBlade.Tests;

public class ExperienceTableTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 83)]
    [InlineData(10, 1154)]
    [InlineData(92, 6_517_253)]
    [InlineData(99, 13_034_431)]
    public void XpForLevel_MatchesCurve(int level, int expectedXp)
    {
        Assert.Equal(expectedXp, ExperienceTable.XpForLevel(level));
    }

    [Fact]
    public void TotalXpToMax_IsThirteenMillion()
    {
        Assert.Equal(13_034_431, ExperienceTable.TotalXpToMax);
    }

    [Fact]
    public void Level92_IsTheHalfwayPoint()
    {
        var ratio = ExperienceTable.XpForLevel(92) / (double)ExperienceTable.XpForLevel(99);
        Assert.InRange(ratio, 0.49, 0.51);
    }

    [Fact]
    public void LevelForXp_IsInverseOfXpForLevel()
    {
        for (var level = 1; level <= 99; level++)
        {
            Assert.Equal(level, ExperienceTable.LevelForXp(ExperienceTable.XpForLevel(level)));
        }
    }

    [Fact]
    public void LevelForXp_HandlesBoundaries()
    {
        Assert.Equal(1, ExperienceTable.LevelForXp(0));
        Assert.Equal(1, ExperienceTable.LevelForXp(82));   // one short of level 2
        Assert.Equal(2, ExperienceTable.LevelForXp(83));
        Assert.Equal(99, ExperienceTable.LevelForXp(99_999_999));
    }
}
