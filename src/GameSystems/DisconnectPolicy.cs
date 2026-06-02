using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems;

/// <summary>A character's combat engagement at the moment they disconnect or request a logout.</summary>
public enum CombatStatus
{
    Neutral,
    CombatEngaged,
}

/// <summary>
/// What the server should do on disconnect: persist immediately and despawn, or keep the body on the
/// grid as a vulnerable dummy for <see cref="GridLockTicks"/> ticks first.
/// </summary>
public readonly record struct DisconnectDecision(bool SaveImmediately, int GridLockTicks)
{
    public bool RemainsAsDummy => GridLockTicks > 0;
}

/// <summary>
/// The combat-logging state machine (Part 20.2). Safe zones always log out cleanly. Otherwise a
/// character that is combat-engaged, or anywhere in the high-risk unrestricted-PvP wilderness, is
/// pinned to the grid as a mindless dummy for 60 ticks (36 s) so attackers can keep rolling against it
/// — neutral players in merely contested (non-wilderness) zones still log out cleanly.
/// </summary>
public static class DisconnectPolicy
{
    /// <summary>60 ticks at the 600 ms heartbeat = 36 seconds.</summary>
    public const int CombatLogoutLockTicks = 60;

    public static DisconnectDecision Evaluate(CombatStatus status, WorldZone zone)
    {
        System.ArgumentNullException.ThrowIfNull(zone);

        if (zone.IsSafeZone)
        {
            return new DisconnectDecision(SaveImmediately: true, GridLockTicks: 0);
        }

        if (status == CombatStatus.CombatEngaged ||
            zone.PlayerVersusPlayer == PlayerVersusPlayerRule.Unrestricted)
        {
            return new DisconnectDecision(SaveImmediately: false, GridLockTicks: CombatLogoutLockTicks);
        }

        return new DisconnectDecision(SaveImmediately: true, GridLockTicks: 0);
    }
}
