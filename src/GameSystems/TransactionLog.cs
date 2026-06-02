using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Communication;

namespace DawnOfBlade.GameSystems;

/// <summary>Every kind of action that moves gold or shifts items between containers (Part 19.2).</summary>
public enum TransactionAction
{
    BankDeposit,
    BankWithdraw,
    MarketBuy,
    MarketSell,
    MonsterDrop,
    GroundPickup,
    ItemDestruction,
    Craft,
}

/// <summary>
/// One audit packet (Part 19.2). Logs who acted, the counterpart, what moved, and the source and
/// destination containers, so duplication exploits and trade loops are traceable after the fact.
/// </summary>
public sealed record TransactionRecord(
    long Timestamp,
    string ActorId,
    string? TargetId,
    TransactionAction Action,
    int ItemId,
    int QuantityChanged,
    string SourceContainerId,
    string DestinationContainerId);

/// <summary>Bus notification raised for each logged transaction.</summary>
public sealed record TransactionLogged(TransactionRecord Record) : IEvent;

/// <summary>
/// Isolated, thread-safe append-only audit log. Any system that alters gold or items dispatches a
/// <see cref="TransactionRecord"/> here; an optional communication bus fans each record out as a
/// <see cref="TransactionLogged"/> event. A production deployment can drain this off-thread to a file
/// or database; the in-memory ledger keeps it deterministic and unit-testable.
/// </summary>
public sealed class TransactionLogger
{
    private readonly object _gate = new();
    private readonly List<TransactionRecord> _records = new();
    private readonly ICommunicationService? _bus;

    public TransactionLogger(ICommunicationService? bus = null) => _bus = bus;

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _records.Count;
            }
        }
    }

    public IReadOnlyList<TransactionRecord> Records
    {
        get
        {
            lock (_gate)
            {
                return _records.ToArray();
            }
        }
    }

    public void Log(TransactionRecord record)
    {
        System.ArgumentNullException.ThrowIfNull(record);
        lock (_gate)
        {
            _records.Add(record);
        }

        _ = _bus?.PublishAsync(new TransactionLogged(record));
    }
}
