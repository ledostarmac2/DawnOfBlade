using System.Collections.Generic;

namespace DawnOfBlade.Simulation;

/// <summary>
/// The commands that resolved on a single tick, handed to every system in registration order.
/// </summary>
public readonly record struct SimulationFrame(SimulationTick Tick, IReadOnlyList<ISimulationCommand> Commands);

/// <summary>
/// Extension point for a tick-resolved subsystem (movement, combat, resource gathering, …).
/// Systems are registered with <see cref="SimulationLoop.AddSystem"/> and executed deterministically
/// in registration order each tick, so domain logic can be layered on without the loop knowing about
/// any concrete rules. Implementations must be deterministic: no wall-clock reads, no unordered
/// iteration, no ambient randomness (inject a seeded source if randomness is required).
/// </summary>
public interface ISimulationSystem
{
    void Execute(in SimulationFrame frame);
}
