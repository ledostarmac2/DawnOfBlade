using DawnOfBlade.Communication;

namespace DawnOfBlade.Simulation;

/// <summary>
/// Published on the communication bus after a tick fully resolves, so observers (HUD, telemetry,
/// a future network relay) can react without coupling to the loop. Carries the resolved tick and
/// how many commands it processed.
/// </summary>
public sealed record SimulationTicked(long Tick, int CommandsProcessed) : IEvent;
