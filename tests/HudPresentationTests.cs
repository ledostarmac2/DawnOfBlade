using DawnOfBlade.UI.Presentation;
using DawnOfBlade.World.Grid;
using Xunit;

namespace DawnOfBlade.Tests;

public class HudPresentationTests
{
    [Fact]
    public void HudPresentationState_FormatsVerifiedCoordinateAndPreservesSelectedTab()
    {
        var state = new HudPresentationState();

        state.ApplyVerifiedTile(new GridCoordinate(1420, 3112));
        state.SelectTab(HudTab.QuestJournal);

        Assert.Equal("Coord: X: 1,420 | Z: 3,112", state.CoordinateText);
        Assert.Equal(HudTab.QuestJournal, state.ActiveTab);
    }

    [Fact]
    public void VitalGaugeState_DelaysThenCatchesUpTrailingDamageBar()
    {
        var state = new VitalGaugeState(100, 100);

        state.Apply(40, 100);
        state.Advance(0.2);
        Assert.Equal(100, state.TrailingValue);

        state.Advance(0.2);
        state.Advance(0.25);
        Assert.InRange(state.TrailingValue, 49.9f, 50.1f);
    }

    [Fact]
    public void RunEnergyState_DisablesAtZeroAndReenablesAtThreshold()
    {
        var state = new RunEnergyState();

        state.ApplyAuthoritative(0, isRunning: true);
        Assert.False(state.IsRunning);
        Assert.False(state.CanToggleRun);

        state.ApplyAuthoritative(14.9f, isRunning: false);
        Assert.False(state.CanToggleRun);

        state.ApplyAuthoritative(15, isRunning: true);
        Assert.True(state.CanToggleRun);
        Assert.True(state.IsRunning);
    }

    [Fact]
    public void HitMarkerPresentation_ClampsOffsetAndExtendsCriticalLifetime()
    {
        var marker = HitMarkerPresentation.Create("wolf-4", 12, HitMarkerType.Critical, 33);

        Assert.Equal(20, marker.HorizontalOffset);
        Assert.Equal(HitMarkerPresentation.CriticalLifetimeSeconds, marker.LifetimeSeconds);
    }
}
