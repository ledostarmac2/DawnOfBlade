using System;

namespace DawnOfBlade.Combat;

/// <summary>
/// A combatant's melee stats (skill levels) plus live hitpoints. Used for both the player and
/// hostile actors. Combat level is an original blend of the four melee skills.
/// </summary>
public sealed class CombatProfile
{
    public CombatProfile(int attack, int strength, int defense, int maxHitpoints)
    {
        Attack = Math.Max(1, attack);
        Strength = Math.Max(1, strength);
        Defense = Math.Max(1, defense);
        MaxHitpoints = Math.Max(1, maxHitpoints);
        CurrentHitpoints = MaxHitpoints;
    }

    public int Attack { get; }
    public int Strength { get; }
    public int Defense { get; }
    public int MaxHitpoints { get; }
    public int CurrentHitpoints { get; private set; }

    public bool IsDefeated => CurrentHitpoints <= 0;

    public int CombatLevel => (int)Math.Round((Attack + Strength + Defense + MaxHitpoints) / 4.0);

    public void ApplyDamage(int amount) =>
        CurrentHitpoints = Math.Max(0, CurrentHitpoints - Math.Max(0, amount));

    public void Heal(int amount) =>
        CurrentHitpoints = Math.Min(MaxHitpoints, CurrentHitpoints + Math.Max(0, amount));

    public void RestoreFull() => CurrentHitpoints = MaxHitpoints;
}
