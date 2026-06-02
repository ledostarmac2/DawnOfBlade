using System.Collections.Generic;

namespace DawnOfBlade.Simulation;

/// <summary>
/// The single-tick input buffer from the blueprint. Commands are queued against a target tick;
/// a command whose target has already passed (arrived late, e.g. network latency) is deferred to
/// the next unprocessed tick rather than dropped. Draining a tick returns its commands in a stable,
/// deterministic order (arrival order, captured by a monotonic sequence number), so the same inputs
/// always resolve identically — the prerequisite for server-authoritative determinism.
/// </summary>
public sealed class TickCommandBuffer
{
    private readonly Dictionary<long, List<Entry>> _byTick = new();
    private long _sequence;

    public int PendingCount
    {
        get
        {
            var total = 0;
            foreach (var list in _byTick.Values)
            {
                total += list.Count;
            }

            return total;
        }
    }

    /// <summary>
    /// Queue a command for <paramref name="targetTick"/>. If that tick is at or before
    /// <paramref name="lastProcessed"/>, the command is moved to the next tick so nothing is lost.
    /// Returns the tick the command will actually resolve on.
    /// </summary>
    public SimulationTick Enqueue(ISimulationCommand command, SimulationTick targetTick, SimulationTick lastProcessed)
    {
        System.ArgumentNullException.ThrowIfNull(command);

        var effective = targetTick.Number <= lastProcessed.Number
            ? lastProcessed.Number + 1
            : targetTick.Number;

        if (!_byTick.TryGetValue(effective, out var list))
        {
            list = new List<Entry>();
            _byTick.Add(effective, list);
        }

        list.Add(new Entry(_sequence++, command));
        return new SimulationTick(effective);
    }

    /// <summary>Removes and returns the commands queued for <paramref name="tick"/>, in arrival order.</summary>
    public IReadOnlyList<ISimulationCommand> Drain(SimulationTick tick)
    {
        if (!_byTick.TryGetValue(tick.Number, out var list))
        {
            return System.Array.Empty<ISimulationCommand>();
        }

        _byTick.Remove(tick.Number);
        list.Sort(static (a, b) => a.Sequence.CompareTo(b.Sequence));

        var commands = new ISimulationCommand[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            commands[i] = list[i].Command;
        }

        return commands;
    }

    private readonly record struct Entry(long Sequence, ISimulationCommand Command);
}
