using System.Collections.Generic;
using DawnOfBlade.Combat;
using DawnOfBlade.Items;
using Xunit;

namespace DawnOfBlade.Tests;

/// <summary>Scripted <see cref="IRandomSource"/> so combat outcomes are deterministic.</summary>
internal sealed class ScriptedRandom : IRandomSource
{
    private readonly Queue<double> _doubles;
    private readonly Queue<int> _ints;

    public ScriptedRandom(double[] doubles, int[] ints)
    {
        _doubles = new Queue<double>(doubles);
        _ints = new Queue<int>(ints);
    }

    public double NextDouble() => _doubles.Dequeue();
    public int Next(int maxExclusive) => _ints.Dequeue();
    public int Next(int minInclusive, int maxExclusive) => _ints.Dequeue();
}

public class CombatTests
{
    [Fact]
    public void CombatProfile_TracksDamageAndDefeat()
    {
        var profile = new CombatProfile(5, 5, 5, 10);
        Assert.False(profile.IsDefeated);

        profile.ApplyDamage(7);
        Assert.Equal(3, profile.CurrentHitpoints);

        profile.ApplyDamage(99);
        Assert.True(profile.IsDefeated);

        profile.RestoreFull();
        Assert.Equal(10, profile.CurrentHitpoints);
    }

    [Fact]
    public void HitChance_ScalesWithAttackVersusDefense()
    {
        var resolver = new CombatResolver(new ScriptedRandom(new double[0], new int[0]));
        var attacker = new CombatProfile(10, 10, 10, 10);
        var defender = new CombatProfile(1, 1, 1, 10);

        var chance = resolver.HitChance(attacker, EquipmentBonuses.Zero, AttackStyle.Aggressive, defender, EquipmentBonuses.Zero);
        Assert.Equal(0.74, chance, 3);

        // Equipment attack bonus raises accuracy.
        var geared = resolver.HitChance(attacker, new EquipmentBonuses(5, 0, 0), AttackStyle.Aggressive, defender, EquipmentBonuses.Zero);
        Assert.True(geared > chance);
    }

    [Fact]
    public void Resolve_LandsAndDealsScriptedDamage()
    {
        // NextDouble 0.1 < hit chance -> lands; Next -> damage roll of 2.
        var resolver = new CombatResolver(new ScriptedRandom(new[] { 0.1 }, new[] { 2 }));
        var attacker = new CombatProfile(10, 10, 10, 10);
        var defender = new CombatProfile(1, 1, 1, 10);

        var result = resolver.Resolve(attacker, EquipmentBonuses.Zero, AttackStyle.Aggressive, defender, EquipmentBonuses.Zero);

        Assert.True(result.Landed);
        Assert.Equal(2, result.Damage);
    }

    [Fact]
    public void Resolve_MissesOnHighRoll()
    {
        var resolver = new CombatResolver(new ScriptedRandom(new[] { 0.99 }, new int[0]));
        var attacker = new CombatProfile(10, 10, 10, 10);
        var defender = new CombatProfile(1, 1, 1, 10);

        var result = resolver.Resolve(attacker, EquipmentBonuses.Zero, AttackStyle.Aggressive, defender, EquipmentBonuses.Zero);

        Assert.False(result.Landed);
        Assert.Equal(0, result.Damage);
    }

    [Fact]
    public void MaxHit_IncreasesWithStrengthGear()
    {
        var resolver = new CombatResolver(new ScriptedRandom(new double[0], new int[0]));
        var attacker = new CombatProfile(1, 10, 1, 10);

        var bare = resolver.MaxHit(attacker, EquipmentBonuses.Zero, AttackStyle.Aggressive);
        var geared = resolver.MaxHit(attacker, new EquipmentBonuses(0, 20, 0), AttackStyle.Aggressive);

        Assert.True(geared > bare);
    }
}
