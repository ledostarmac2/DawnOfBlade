using System;
using System.Threading;
using System.Threading.Tasks;

namespace DawnOfBlade.Engine.Tick;

/// <summary>
/// Drives a <see cref="TickEngine"/> on the real 600 ms cadence. The loop is decoupled from the
/// engine's deterministic <see cref="TickEngine.ProcessTick"/> so headless servers and tests can
/// step ticks manually while production drives them on a wall clock.
/// </summary>
public sealed class TickLoop
{
    private readonly TickEngine _engine;

    public TickLoop(TickEngine engine) => _engine = engine;

    /// <summary>Raised after each tick is fully processed.</summary>
    public event Action<TickContext>? TickCompleted;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TickEngine.TickDurationMs));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var context = _engine.ProcessTick();
            TickCompleted?.Invoke(context);
        }
    }
}
