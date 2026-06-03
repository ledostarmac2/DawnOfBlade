namespace DawnOfBlade.Engine.Tick;

/// <summary>
/// A Phase 1 consumption action (item/resource use, healing). All consumption actions queued
/// in the same tick resolve together: pairing one action that triggers an animation delay with
/// one standard action lets both apply in the same 600 ms frame before any consecutive-use
/// cooldown gate engages on the following tick ("combo intake").
/// </summary>
public sealed class ConsumptionAction : ITickAction
{
    private readonly System.Action _apply;

    public ConsumptionAction(bool triggersAnimationDelay, System.Action apply)
    {
        TriggersAnimationDelay = triggersAnimationDelay;
        _apply = apply;
    }

    public TickPhase Phase => TickPhase.Consumption;

    /// <summary>Whether using this item plays a use-animation that would normally gate the next use.</summary>
    public bool TriggersAnimationDelay { get; }

    public void Execute(TickContext context) => _apply();
}
