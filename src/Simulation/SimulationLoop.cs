using System.Collections.Generic;
using DawnOfBlade.Communication;

namespace DawnOfBlade.Simulation;

/// <summary>
/// The deterministic, engine-independent core of the blueprint's server-authoritative tick loop.
/// Each <see cref="Advance"/> call steps exactly one tick forward (monotonic), drains the commands
/// buffered for that tick in a stable order, and runs every registered <see cref="ISimulationSystem"/>
/// in registration order. Given the same registered systems and the same scheduled commands, the
/// resulting frames are identical run-to-run — the property the netcode relies on for client
/// reconciliation and rubber-banding.
///
/// It owns no clock and no threads: the host (a Godot node, a server worker, or a test) decides when
/// to advance, typically driven by <see cref="SimulationClock"/>. An optional
/// <see cref="ICommunicationService"/> lets the loop announce each completed tick via
/// <see cref="SimulationTicked"/> without coupling to any consumer.
/// </summary>
public sealed class SimulationLoop
{
    private readonly TickCommandBuffer _buffer = new();
    private readonly List<ISimulationSystem> _systems = new();
    private readonly ICommunicationService? _bus;

    public SimulationLoop(ICommunicationService? bus = null)
    {
        _bus = bus;
    }

    /// <summary>The last tick that has been fully resolved. Starts at <see cref="SimulationTick.Zero"/>.</summary>
    public SimulationTick CurrentTick { get; private set; } = SimulationTick.Zero;

    /// <summary>Commands buffered but not yet resolved.</summary>
    public int PendingCommandCount => _buffer.PendingCount;

    /// <summary>Registers a system. Systems execute in the order added, every tick.</summary>
    public void AddSystem(ISimulationSystem system)
    {
        System.ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
    }

    /// <summary>Schedule a command for a specific tick (late commands roll to the next tick).</summary>
    public SimulationTick Schedule(ISimulationCommand command, SimulationTick targetTick) =>
        _buffer.Enqueue(command, targetTick, CurrentTick);

    /// <summary>Schedule a command for the next tick — the common path for fresh client input.</summary>
    public SimulationTick ScheduleNext(ISimulationCommand command) =>
        _buffer.Enqueue(command, CurrentTick.Next(), CurrentTick);

    /// <summary>
    /// Advance one tick: increment, drain that tick's commands, run systems in order, then announce
    /// the completed tick. Commands a system schedules during execution target a future tick (they
    /// arrive "late" for the tick in flight), preventing same-tick re-entrancy.
    /// </summary>
    public SimulationFrame Advance()
    {
        CurrentTick = CurrentTick.Next();
        var commands = _buffer.Drain(CurrentTick);
        var frame = new SimulationFrame(CurrentTick, commands);

        foreach (var system in _systems)
        {
            system.Execute(in frame);
        }

        // Fire-and-forget: the in-process bus dispatches synchronously, so observers see the tick
        // immediately, while a future transport can forward it asynchronously.
        _ = _bus?.PublishAsync(new SimulationTicked(CurrentTick.Number, commands.Count));

        return frame;
    }

    /// <summary>Advance <paramref name="ticks"/> ticks in order (e.g. to catch up after a long frame).</summary>
    public void Advance(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            Advance();
        }
    }
}
