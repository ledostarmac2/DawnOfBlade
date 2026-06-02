namespace DawnOfBlade.Simulation;

/// <summary>
/// A monotonic simulation tick number. The blueprint's authoritative loop advances exactly once
/// per <see cref="SimulationClock.TickDuration"/> (600 ms); this value never moves backwards.
/// </summary>
public readonly record struct SimulationTick(long Number)
{
    public static readonly SimulationTick Zero = new(0);

    public SimulationTick Next() => new(Number + 1);

    public bool IsAfter(SimulationTick other) => Number > other.Number;

    public override string ToString() => $"tick {Number}";
}
