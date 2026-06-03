using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Tick;

/// <summary>
/// The authoritative discrete heartbeat. Client actions are batched between ticks and applied
/// in one deterministic sequence at each tick boundary: sorted by <see cref="TickPhase"/>, then
/// by submission order within a phase. <see cref="ProcessTick"/> is synchronous and
/// side-effect-isolated, which keeps the simulation reproducible and testable.
/// </summary>
public sealed class TickEngine
{
    /// <summary>The absolute tick interval, in milliseconds.</summary>
    public const int TickDurationMs = 600;

    private readonly List<Scheduled> _pending = new();
    private long _sequence;

    public long CurrentTick { get; private set; }

    public int PendingCount => _pending.Count;

    /// <summary>Batches an action for execution at the next tick boundary.</summary>
    public void Enqueue(ITickAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _pending.Add(new Scheduled(action, _sequence++));
    }

    /// <summary>Sorts the batched actions by phase then submission order and executes them.</summary>
    public TickContext ProcessTick()
    {
        var context = new TickContext(CurrentTick);

        _pending.Sort(static (left, right) =>
        {
            var byPhase = ((int)left.Action.Phase).CompareTo((int)right.Action.Phase);
            return byPhase != 0 ? byPhase : left.Sequence.CompareTo(right.Sequence);
        });

        foreach (var scheduled in _pending)
        {
            scheduled.Action.Execute(context);
        }

        _pending.Clear();
        CurrentTick++;
        return context;
    }

    private readonly record struct Scheduled(ITickAction Action, long Sequence);
}
