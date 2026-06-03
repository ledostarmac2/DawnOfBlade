using System.Collections.Generic;
using DawnOfBlade.Engine.Tick;
using Xunit;

namespace DawnOfBlade.Tests;

public class TickEngineTests
{
    [Fact]
    public void ProcessTick_ExecutesByPhaseThenSubmissionOrder()
    {
        var engine = new TickEngine();
        var log = new List<string>();

        // Enqueue out of phase order; combat first, interface last.
        engine.Enqueue(new DelegateAction(TickPhase.Combat, _ => log.Add("combat")));
        engine.Enqueue(new DelegateAction(TickPhase.Movement, _ => log.Add("move")));
        engine.Enqueue(new DelegateAction(TickPhase.Interface, _ => log.Add("ui-1")));
        engine.Enqueue(new DelegateAction(TickPhase.Interface, _ => log.Add("ui-2")));
        engine.Enqueue(new DelegateAction(TickPhase.Consumption, _ => log.Add("eat")));

        engine.ProcessTick();

        Assert.Equal(new[] { "ui-1", "ui-2", "eat", "move", "combat" }, log);
    }

    [Fact]
    public void Consumption_ComboIntakeAppliesBothInSameTick()
    {
        var engine = new TickEngine();
        var hp = 10;

        // One animation-delay action paired with one standard action in the same 600 ms frame.
        engine.Enqueue(new ConsumptionAction(triggersAnimationDelay: true, () => hp += 7));
        engine.Enqueue(new ConsumptionAction(triggersAnimationDelay: false, () => hp += 4));

        engine.ProcessTick();

        Assert.Equal(21, hp);
        Assert.Equal(0, engine.PendingCount);
    }

    [Fact]
    public void Mitigation_StateReadAtCombatPhase_HonorsSameTickToggle()
    {
        var engine = new TickEngine();
        var overhead = new OverheadMitigation(MitigationKind.Melee, active: true);
        bool? blockedAtDamageTime = null;

        // Phase 0 toggles the overhead off; Phase 3 reads it at damage time.
        engine.Enqueue(new DelegateAction(TickPhase.Combat, _ => blockedAtDamageTime = overhead.Blocks(MitigationKind.Melee)));
        engine.Enqueue(new DelegateAction(TickPhase.Interface, _ => overhead.IsActive = false));

        engine.ProcessTick();

        Assert.False(blockedAtDamageTime);
    }

    [Fact]
    public void CurrentTick_Advances()
    {
        var engine = new TickEngine();
        Assert.Equal(0, engine.CurrentTick);

        engine.ProcessTick();
        engine.ProcessTick();

        Assert.Equal(2, engine.CurrentTick);
    }
}
