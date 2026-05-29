using System;

namespace DawnOfBlade.Combat;

public sealed class CombatStats
{
    public int Attack { get; set; } = 1;
    public int Defense { get; set; } = 1;
    public int MaxHitpoints { get; set; } = 10;
    public int CurrentHitpoints { get; private set; } = 10;

    public bool IsDefeated => CurrentHitpoints <= 0;

    public void ApplyDamage(int amount)
    {
        CurrentHitpoints = Math.Max(0, CurrentHitpoints - Math.Max(0, amount));
    }

    public void Heal(int amount)
    {
        CurrentHitpoints = Math.Min(MaxHitpoints, CurrentHitpoints + Math.Max(0, amount));
    }
}
