using System;
using DawnOfBlade.Items;

namespace DawnOfBlade.Combat;

/// <summary>The outcome of one attack: whether it landed and how much damage it dealt.</summary>
public readonly record struct HitResult(bool Landed, int Damage);

/// <summary>
/// Resolves a single melee attack using an original accuracy/damage model:
/// accuracy scales with the attacker's effective attack minus the defender's effective defense,
/// and max hit scales with the attacker's effective strength. All randomness goes through
/// <see cref="IRandomSource"/> so fights are reproducible in tests.
/// </summary>
public sealed class CombatResolver
{
    private readonly IRandomSource _random;

    public CombatResolver(IRandomSource random) => _random = random;

    public double HitChance(CombatProfile attacker, EquipmentBonuses attackerGear, AttackStyle style, CombatProfile defender, EquipmentBonuses defenderGear)
    {
        var accuracy = attacker.Attack + attackerGear.Attack + StyleAccuracyBonus(style);
        var evasion = defender.Defense + defenderGear.Defense + 1;
        var chance = 0.5 + 0.03 * (accuracy - evasion);
        return Math.Clamp(chance, 0.05, 0.95);
    }

    public int MaxHit(CombatProfile attacker, EquipmentBonuses attackerGear, AttackStyle style)
    {
        var power = attacker.Strength + attackerGear.Strength + StyleStrengthBonus(style);
        return Math.Max(1, (int)Math.Round(power * 0.15) + 1);
    }

    public HitResult Resolve(CombatProfile attacker, EquipmentBonuses attackerGear, AttackStyle style, CombatProfile defender, EquipmentBonuses defenderGear)
    {
        if (_random.NextDouble() > HitChance(attacker, attackerGear, style, defender, defenderGear))
        {
            return new HitResult(false, 0);
        }

        var maxHit = MaxHit(attacker, attackerGear, style);
        var damage = _random.Next(0, maxHit + 1);
        return new HitResult(true, damage);
    }

    private static int StyleAccuracyBonus(AttackStyle style) => style == AttackStyle.Accurate ? 3 : 0;

    private static int StyleStrengthBonus(AttackStyle style) => style == AttackStyle.Aggressive ? 3 : 0;
}
