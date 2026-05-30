using DawnOfBlade.Skills;
using Xunit;

namespace DawnOfBlade.Tests;

public class SkillProgressTests
{
    [Fact]
    public void StartsAtLevelOne()
    {
        Assert.Equal(1, new SkillProgress("foraging").Level);
    }

    [Fact]
    public void ReachesLevelTwoAtThreshold()
    {
        var threshold = SkillProgress.ExperienceForLevel(2);

        Assert.Equal(1, new SkillProgress("foraging", threshold - 1).Level);
        Assert.Equal(2, new SkillProgress("foraging", threshold).Level);
    }

    [Fact]
    public void AddExperience_NeverGoesNegative()
    {
        var skill = new SkillProgress("foraging");
        skill.AddExperience(-100);

        Assert.Equal(0, skill.Experience);
    }

    [Fact]
    public void ExperienceCurveIsMonotonic()
    {
        for (var level = 2; level <= 99; level++)
        {
            Assert.True(SkillProgress.ExperienceForLevel(level) >= SkillProgress.ExperienceForLevel(level - 1));
        }
    }
}
