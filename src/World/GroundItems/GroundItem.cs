using DawnOfBlade.World.Grid;

namespace DawnOfBlade.World.GroundItems;

public enum GroundItemPhase
{
    Private,
    Public,
    Expired,
}

/// <summary>Server-owned dropped item visibility lifecycle measured in deterministic simulation ticks.</summary>
public sealed record GroundItem(
    string Id,
    string ItemId,
    int Quantity,
    GridCoordinate Tile,
    string? PrivateOwnerId,
    long DroppedAtTick)
{
    public const int PrivateWindowTicks = 100;
    public const int PublicWindowTicks = 100;
    public const int TotalLifetimeTicks = PrivateWindowTicks + PublicWindowTicks;

    public GroundItemPhase PhaseAt(long currentTick)
    {
        var age = System.Math.Max(0, currentTick - DroppedAtTick);
        return age switch
        {
            < PrivateWindowTicks => GroundItemPhase.Private,
            < TotalLifetimeTicks => GroundItemPhase.Public,
            _ => GroundItemPhase.Expired,
        };
    }

    public bool IsVisibleTo(string playerId, long currentTick) =>
        PhaseAt(currentTick) switch
        {
            GroundItemPhase.Private => PrivateOwnerId == playerId,
            GroundItemPhase.Public => true,
            _ => false,
        };
}
