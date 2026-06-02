namespace DawnOfBlade.Communication;

/// <summary>
/// Domain events published onto the <see cref="ICommunicationService"/> bus as gameplay happens.
/// They are immutable so a future server adapter can serialize and forward them unchanged. Today
/// they fan out to in-process subscribers (HUD notices, telemetry); tomorrow they can cross a wire.
/// </summary>
public sealed record ResourceGathered(string ItemId, string SkillId, int Experience) : IEvent;

public sealed record SkillLeveledUp(string SkillId, int Level) : IEvent;

public sealed record EnemyDefeated(string EnemyName, int CoinReward) : IEvent;
