namespace DawnOfBlade.Engine.Ai;

/// <summary>
/// Decides whether an actor of a given <see cref="MonsterArchetype"/> will initiate combat with a
/// target, based on distance and the two parties' combat levels. This is the single rule both the
/// AI brain and any tooltip/threat indicator should consult, so "will it attack me?" is answered
/// identically everywhere.
/// </summary>
public static class AggressionPolicy
{
    /// <summary>
    /// An <see cref="MonsterArchetype.Aggressive"/> monster ignores a target once that target's
    /// combat level exceeds <c>DisparityMultiplier * selfLevel + 1</c>. With a multiplier of 2 a
    /// level-10 monster stops being a threat to players above combat level 21.
    /// </summary>
    public const int DisparityMultiplier = 2;

    public static bool WillEngage(
        MonsterArchetype archetype,
        int selfCombatLevel,
        int targetCombatLevel,
        int distanceTiles,
        int aggroRadius)
    {
        if (distanceTiles > aggroRadius || aggroRadius <= 0)
        {
            return false;
        }

        return archetype switch
        {
            MonsterArchetype.Aggressive => targetCombatLevel <= (DisparityMultiplier * selfCombatLevel) + 1,
            MonsterArchetype.Predator => true,
            _ => false, // Passive and Defensive never initiate.
        };
    }
}
