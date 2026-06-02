using System.Collections.Generic;
using System.Threading.Tasks;
using DawnOfBlade.Communication;
using DawnOfBlade.Simulation;
using Xunit;

namespace DawnOfBlade.Tests;

public class SimulationTests
{
    private sealed record MoveStep(string ActorId, int X, int Z) : ISimulationCommand;

    private sealed class RecordingSystem : ISimulationSystem
    {
        public List<(long Tick, string Tag)> Seen { get; } = new();
        private readonly string _tag;

        public RecordingSystem(string tag) => _tag = tag;

        public void Execute(in SimulationFrame frame)
        {
            foreach (var command in frame.Commands)
            {
                if (command is MoveStep move)
                {
                    Seen.Add((frame.Tick.Number, $"{_tag}:{move.ActorId}"));
                }
            }
        }
    }

    [Fact]
    public void Advance_IncrementsTickMonotonically()
    {
        var loop = new SimulationLoop();
        Assert.Equal(0, loop.CurrentTick.Number);

        Assert.Equal(1, loop.Advance().Tick.Number);
        Assert.Equal(2, loop.Advance().Tick.Number);
        loop.Advance(3);
        Assert.Equal(5, loop.CurrentTick.Number);
    }

    [Fact]
    public void ScheduledCommand_ResolvesOnItsTargetTick()
    {
        var loop = new SimulationLoop();
        var system = new RecordingSystem("s");
        loop.AddSystem(system);

        var resolved = loop.Schedule(new MoveStep("p1", 1, 1), new SimulationTick(3));
        Assert.Equal(3, resolved.Number);

        loop.Advance(); // tick 1 - nothing
        loop.Advance(); // tick 2 - nothing
        Assert.Empty(system.Seen);
        loop.Advance(); // tick 3 - resolves
        Assert.Equal(new[] { (3L, "s:p1") }, system.Seen);
    }

    [Fact]
    public void LateCommand_IsDeferredToNextTick_NotDropped()
    {
        var loop = new SimulationLoop();
        var system = new RecordingSystem("s");
        loop.AddSystem(system);

        loop.Advance(); // CurrentTick = 1
        loop.Advance(); // CurrentTick = 2

        // Targets tick 1, which already passed -> must roll forward to tick 3 (CurrentTick + 1).
        var resolved = loop.Schedule(new MoveStep("late", 0, 0), new SimulationTick(1));
        Assert.Equal(3, resolved.Number);

        var frame = loop.Advance(); // tick 3
        Assert.Equal(new[] { (3L, "s:late") }, system.Seen);
        Assert.Single(frame.Commands);
    }

    [Fact]
    public void CommandsWithinATick_DrainInDeterministicArrivalOrder()
    {
        var loop = new SimulationLoop();
        var system = new RecordingSystem("s");
        loop.AddSystem(system);

        loop.Schedule(new MoveStep("a", 0, 0), new SimulationTick(1));
        loop.Schedule(new MoveStep("b", 0, 0), new SimulationTick(1));
        loop.Schedule(new MoveStep("c", 0, 0), new SimulationTick(1));

        loop.Advance();

        Assert.Equal(new[] { (1L, "s:a"), (1L, "s:b"), (1L, "s:c") }, system.Seen);
    }

    [Fact]
    public void Systems_ExecuteInRegistrationOrder()
    {
        var loop = new SimulationLoop();
        var first = new RecordingSystem("first");
        var second = new RecordingSystem("second");
        loop.AddSystem(first);
        loop.AddSystem(second);

        loop.ScheduleNext(new MoveStep("p", 0, 0));
        loop.Advance();

        Assert.Equal(new[] { (1L, "first:p") }, first.Seen);
        Assert.Equal(new[] { (1L, "second:p") }, second.Seen);
    }

    [Fact]
    public void Clock_AccumulatesWholeTicksAndKeepsRemainder()
    {
        var clock = new SimulationClock();

        Assert.Equal(0, clock.Accumulate(300));     // half a tick
        Assert.Equal(1, clock.Accumulate(300));     // completes one tick
        Assert.Equal(2, clock.Accumulate(1300));    // 1300 + 0 -> 2 ticks, 100ms remainder
        Assert.Equal(100, clock.PendingMilliseconds, 3);
        Assert.Equal(3, clock.TicksElapsed);
    }

    [Fact]
    public void Clock_IgnoresNonPositiveDeltas_StayingMonotonic()
    {
        var clock = new SimulationClock();
        clock.Accumulate(600);

        Assert.Equal(0, clock.Accumulate(0));
        Assert.Equal(0, clock.Accumulate(-5000));
        Assert.Equal(1, clock.TicksElapsed);
    }

    [Fact]
    public async Task Advance_PublishesSimulationTickedOnTheBus()
    {
        var bus = new InProcessCommunicationService();
        var ticks = new List<SimulationTicked>();
        bus.Subscribe<SimulationTicked>((envelope, _) =>
        {
            ticks.Add(envelope.Message);
            return ValueTask.CompletedTask;
        });

        var loop = new SimulationLoop(bus);
        loop.ScheduleNext(new MoveStep("p", 2, 2));
        loop.Advance();
        loop.Advance();

        await Task.CompletedTask;
        Assert.Equal(2, ticks.Count);
        Assert.Equal(1, ticks[0].Tick);
        Assert.Equal(1, ticks[0].CommandsProcessed);
        Assert.Equal(2, ticks[1].Tick);
        Assert.Equal(0, ticks[1].CommandsProcessed);
    }
}
