namespace DawnOfBlade.Combat;

/// <summary>Tracks the wilderness aggressor penalty window in simulation ticks.</summary>
public sealed class AggressorStatus
{
    public const int DurationTicks = 2000;

    public long? ExpiresAtTick { get; private set; }

    public bool IsActive(long currentTick) => ExpiresAtTick is { } expiry && currentTick < expiry;

    public void ApplyAt(long currentTick)
    {
        ExpiresAtTick = currentTick + DurationTicks;
    }

    public void Clear()
    {
        ExpiresAtTick = null;
    }
}
