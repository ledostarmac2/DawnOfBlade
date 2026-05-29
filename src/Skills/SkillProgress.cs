using System;

namespace DawnOfBlade.Skills;

public sealed class SkillProgress
{
    public string SkillId { get; }
    public int Experience { get; private set; }
    public int Level => CalculateLevel(Experience);

    public SkillProgress(string skillId, int experience = 0)
    {
        SkillId = skillId;
        Experience = Math.Max(0, experience);
    }

    public void AddExperience(int amount)
    {
        Experience = Math.Max(0, Experience + Math.Max(0, amount));
    }

    public static int CalculateLevel(int experience)
    {
        var level = 1;

        while (level < 99 && experience >= ExperienceForLevel(level + 1))
        {
            level++;
        }

        return level;
    }

    public static int ExperienceForLevel(int level)
    {
        level = Math.Clamp(level, 1, 99);
        return (int)Math.Round(75 * Math.Pow(level - 1, 2.15));
    }
}

