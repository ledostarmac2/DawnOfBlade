namespace DawnOfBlade.Engine.Combat;

/// <summary>
/// Pure combat math for the probabilistic resolver. The "max roll" is the size of each side's
/// accuracy roll space; the closed-form hit probability is equivalent to comparing a uniform
/// integer roll in <c>[0, A]</c> against one in <c>[0, D]</c> (attacker wins on a strict greater).
/// </summary>
public static class CombatFormulas
{
    /// <summary>MaxRoll = EffectiveLevel * (EquipmentBonus + 64).</summary>
    public static int MaxRoll(int effectiveLevel, int equipmentBonus) =>
        effectiveLevel * (equipmentBonus + 64);

    /// <summary>
    /// Closed-form probability that an attack with attacker max roll <paramref name="attackRoll"/>
    /// lands against defender max roll <paramref name="defenceRoll"/>.
    /// </summary>
    public static double HitChance(int attackRoll, int defenceRoll)
    {
        double a = attackRoll;
        double d = defenceRoll;

        return attackRoll > defenceRoll
            ? 1.0 - (d + 2.0) / (2.0 * (a + 1.0))
            : a / (2.0 * (d + 1.0));
    }
}
