namespace DawnOfBlade.Simulation;

/// <summary>
/// Converts elapsed real time into whole simulation ticks. The Godot client renders at its native
/// framerate and feeds frame deltas here; the clock accumulates them and reports how many fixed
/// 600 ms ticks are due, holding the remainder for the next frame. Time only moves forward — a
/// negative or zero delta yields zero ticks — so <see cref="TicksElapsed"/> is strictly monotonic.
/// </summary>
public sealed class SimulationClock
{
    /// <summary>The blueprint's deterministic heartbeat: one tick every 600 ms.</summary>
    public static readonly System.TimeSpan TickDuration = System.TimeSpan.FromMilliseconds(600);

    private readonly double _tickMilliseconds;
    private double _accumulatedMilliseconds;

    public SimulationClock(System.TimeSpan? tickDuration = null)
    {
        var duration = tickDuration ?? TickDuration;
        if (duration <= System.TimeSpan.Zero)
        {
            throw new System.ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be positive.");
        }

        _tickMilliseconds = duration.TotalMilliseconds;
    }

    /// <summary>Total ticks reported since construction. Never decreases.</summary>
    public long TicksElapsed { get; private set; }

    /// <summary>Unconsumed time held toward the next tick, in milliseconds.</summary>
    public double PendingMilliseconds => _accumulatedMilliseconds;

    /// <summary>
    /// Adds elapsed real time and returns how many whole ticks are now due. Non-positive deltas
    /// add nothing and return 0, keeping the simulation from ever stepping backwards.
    /// </summary>
    public int Accumulate(double deltaMilliseconds)
    {
        if (deltaMilliseconds <= 0)
        {
            return 0;
        }

        _accumulatedMilliseconds += deltaMilliseconds;

        var due = 0;
        while (_accumulatedMilliseconds >= _tickMilliseconds)
        {
            _accumulatedMilliseconds -= _tickMilliseconds;
            due++;
            TicksElapsed++;
        }

        return due;
    }
}
