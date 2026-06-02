namespace DawnOfBlade.World;

/// <summary>Engine-independent tick lifecycle for a depleting world resource.</summary>
public sealed class ResourceRespawnState
{
    public bool IsDepleted { get; private set; }
    public long RespawnsAtTick { get; private set; }

    public void Deplete(long currentTick, int respawnTicks)
    {
        IsDepleted = true;
        RespawnsAtTick = currentTick + System.Math.Max(1, respawnTicks);
    }

    public bool AdvanceTick(long currentTick)
    {
        if (!IsDepleted || currentTick < RespawnsAtTick)
        {
            return false;
        }

        IsDepleted = false;
        return true;
    }
}
