using System;

namespace DawnOfBlade.Engine.Progression;

/// <summary>
/// Maps experience to skill levels on a steep exponential curve capped at level 99. The
/// cumulative XP required to reach level L is:
/// <code>
/// XP(L) = floor( 0.25 * Σ_{i=1}^{L-1} floor( i + 300 * 2^(i/7) ) )
/// </code>
/// The table is precomputed once. By construction level 92 is the ~50% midpoint of the journey
/// to level 99 (XP(92) = 6,517,253 vs XP(99) = 13,034,431).
/// </summary>
public static class ExperienceTable
{
    public const int MinLevel = 1;
    public const int MaxLevel = 99;

    // _xpForLevel[L] = cumulative XP required to reach level L. Index 0 unused.
    private static readonly int[] _xpForLevel = BuildTable();

    /// <summary>Total XP required to reach the level cap.</summary>
    public static int TotalXpToMax => _xpForLevel[MaxLevel];

    /// <summary>Cumulative XP required to reach <paramref name="level"/> (clamped to 1..99).</summary>
    public static int XpForLevel(int level)
    {
        level = Math.Clamp(level, MinLevel, MaxLevel);
        return _xpForLevel[level];
    }

    /// <summary>XP needed to advance from <paramref name="level"/> to the next level.</summary>
    public static int XpToNextLevel(int level)
    {
        level = Math.Clamp(level, MinLevel, MaxLevel);
        return level >= MaxLevel ? 0 : _xpForLevel[level + 1] - _xpForLevel[level];
    }

    /// <summary>The level reached at <paramref name="experience"/> XP (1..99).</summary>
    public static int LevelForXp(int experience)
    {
        if (experience <= 0)
        {
            return MinLevel;
        }

        var level = MinLevel;
        while (level < MaxLevel && experience >= _xpForLevel[level + 1])
        {
            level++;
        }

        return level;
    }

    private static int[] BuildTable()
    {
        var table = new int[MaxLevel + 1];
        table[MinLevel] = 0;

        double accumulator = 0;
        for (var level = MinLevel; level < MaxLevel; level++)
        {
            accumulator += Math.Floor(level + 300.0 * Math.Pow(2.0, level / 7.0));
            table[level + 1] = (int)Math.Floor(accumulator / 4.0);
        }

        return table;
    }
}
