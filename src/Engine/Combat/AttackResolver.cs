using DawnOfBlade.Combat;

namespace DawnOfBlade.Engine.Combat;

/// <summary>The result of one resolved attack.</summary>
/// <param name="Accurate">True if the accuracy roll succeeded (the attack landed).</param>
/// <param name="Damage">Damage dealt; 0 on a miss, and also possibly 0 on a landed hit.</param>
public readonly record struct AttackOutcome(bool Accurate, int Damage)
{
    /// <summary>A landed hit that rolled 0 on the damage spectrum — distinct from a miss.</summary>
    public bool IsZeroDamageHit => Accurate && Damage == 0;
}

/// <summary>
/// Two-phase probabilistic attack resolution. Phase 1 rolls a uniform integer in <c>[0, A]</c>
/// for the attacker and <c>[0, D]</c> for the defender; the attack lands when the attacker's roll
/// is strictly greater. Phase 2, only on a landing hit, rolls damage uniformly in <c>[0, MaxHit]</c>.
/// A miss yields a forced 0; a landed hit that rolls 0 is reported as accurate with 0 damage.
/// All randomness flows through <see cref="IRandomSource"/> for reproducible tests.
/// </summary>
public sealed class AttackResolver
{
    private readonly IRandomSource _random;

    public AttackResolver(IRandomSource random) => _random = random;

    public AttackOutcome Resolve(int attackMaxRoll, int defenceMaxRoll, int maxHit)
    {
        // Phase 1 — accuracy. Next(0, n + 1) yields an inclusive [0, n] roll.
        var attackRoll = _random.Next(0, System.Math.Max(0, attackMaxRoll) + 1);
        var defenceRoll = _random.Next(0, System.Math.Max(0, defenceMaxRoll) + 1);

        if (attackRoll <= defenceRoll)
        {
            return new AttackOutcome(Accurate: false, Damage: 0);
        }

        // Phase 2 — damage.
        var damage = _random.Next(0, System.Math.Max(0, maxHit) + 1);
        return new AttackOutcome(Accurate: true, Damage: damage);
    }
}
