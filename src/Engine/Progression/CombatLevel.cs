using System;

namespace DawnOfBlade.Engine.Progression;

/// <summary>
/// The single source of truth for an actor's combat level. Both the player and every monster
/// derive their level from the same melee blend so that aggression and matchmaking comparisons
/// (see <c>Engine.Ai.AggressionPolicy</c>) are symmetric.
/// <para>
/// The level is a weighted sum of a <em>defensive</em> base (Defense + Hitpoints) and an
/// <em>offensive</em> term (Attack + Strength):
/// <code>
/// level = floor( 0.25 * (Defense + Hitpoints) + 0.325 * (Attack + Strength) )
/// </code>
/// With all four skills capped at 99 this yields a maximum combat level of 113. Each input is
/// clamped to a floor of 1 so a partially-defined profile never produces a level below 1.
/// </para>
/// </summary>
public static class CombatLevel
{
    private const double DefensiveWeight = 0.25;
    private const double OffensiveWeight = 0.325;

    public static int Compute(int attack, int strength, int defense, int hitpoints)
    {
        var a = Math.Max(1, attack);
        var s = Math.Max(1, strength);
        var d = Math.Max(1, defense);
        var hp = Math.Max(1, hitpoints);

        var defensive = DefensiveWeight * (d + hp);
        var offensive = OffensiveWeight * (a + s);
        return Math.Max(1, (int)Math.Floor(defensive + offensive));
    }
}
