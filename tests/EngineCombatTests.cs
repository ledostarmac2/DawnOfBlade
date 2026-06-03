using DawnOfBlade.Engine.Combat;
using Xunit;

namespace DawnOfBlade.Tests;

public class EngineCombatTests
{
    [Fact]
    public void MaxRoll_IsEffectiveLevelTimesBonusPlus64()
    {
        Assert.Equal(99 * (80 + 64), CombatFormulas.MaxRoll(99, 80));
    }

    [Fact]
    public void HitChance_UsesGreaterBranchWhenAttackExceedsDefence()
    {
        // A > D: 1 - (D + 2) / (2 (A + 1)) = 1 - 52 / 202
        Assert.Equal(1.0 - 52.0 / 202.0, CombatFormulas.HitChance(100, 50), 6);
    }

    [Fact]
    public void HitChance_UsesLesserBranchWhenDefenceMeetsOrExceedsAttack()
    {
        // A <= D: A / (2 (D + 1)) = 50 / 202
        Assert.Equal(50.0 / 202.0, CombatFormulas.HitChance(50, 100), 6);
        // Equality also takes the lesser branch.
        Assert.Equal(10.0 / 22.0, CombatFormulas.HitChance(10, 10), 6);
    }

    [Fact]
    public void Resolve_AccuracyMissForcesZeroDamage()
    {
        // attackRoll 0 <= defenceRoll 5 -> miss; no damage roll consumed.
        var resolver = new AttackResolver(new ScriptedRandom(new double[0], new[] { 0, 5 }));

        var outcome = resolver.Resolve(attackMaxRoll: 100, defenceMaxRoll: 100, maxHit: 10);

        Assert.False(outcome.Accurate);
        Assert.Equal(0, outcome.Damage);
        Assert.False(outcome.IsZeroDamageHit);
    }

    [Fact]
    public void Resolve_AccurateHitRollsDamage()
    {
        // attackRoll 10 > defenceRoll 2 -> hit; damage roll 4.
        var resolver = new AttackResolver(new ScriptedRandom(new double[0], new[] { 10, 2, 4 }));

        var outcome = resolver.Resolve(attackMaxRoll: 100, defenceMaxRoll: 100, maxHit: 10);

        Assert.True(outcome.Accurate);
        Assert.Equal(4, outcome.Damage);
    }

    [Fact]
    public void Resolve_DistinguishesZeroDamageHitFromMiss()
    {
        // Hit (10 > 2) but damage roll lands on 0.
        var resolver = new AttackResolver(new ScriptedRandom(new double[0], new[] { 10, 2, 0 }));

        var outcome = resolver.Resolve(attackMaxRoll: 100, defenceMaxRoll: 100, maxHit: 10);

        Assert.True(outcome.Accurate);
        Assert.Equal(0, outcome.Damage);
        Assert.True(outcome.IsZeroDamageHit);
    }
}
