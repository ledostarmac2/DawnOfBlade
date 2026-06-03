namespace DawnOfBlade.Engine.Tick;

/// <summary>Context passed to actions as they execute within a tick.</summary>
public sealed class TickContext
{
    public TickContext(long tick) => Tick = tick;

    /// <summary>The tick number being processed (monotonic).</summary>
    public long Tick { get; }
}

/// <summary>
/// A batched, deterministic action resolved at a tick boundary. The engine groups actions by
/// <see cref="Phase"/> and runs them in submission order within a phase.
/// </summary>
public interface ITickAction
{
    TickPhase Phase { get; }

    void Execute(TickContext context);
}

/// <summary>A lightweight <see cref="ITickAction"/> built from a delegate.</summary>
public sealed class DelegateAction : ITickAction
{
    private readonly System.Action<TickContext> _execute;

    public DelegateAction(TickPhase phase, System.Action<TickContext> execute)
    {
        Phase = phase;
        _execute = execute;
    }

    public TickPhase Phase { get; }

    public void Execute(TickContext context) => _execute(context);
}
