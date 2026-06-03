using System.Collections.Generic;
using DawnOfBlade.Engine.Economy;
using Xunit;

namespace DawnOfBlade.Tests;

public class EconomyTests
{
    [Fact]
    public void GridInventory_HasHard28SlotCeiling()
    {
        var inventory = new GridInventory();

        Assert.True(inventory.TryAdd("starsteel_greaves", 28, stackable: false));
        Assert.Equal(0, inventory.FreeSlots);

        // No room for a 29th non-stackable item or a new stackable item.
        Assert.False(inventory.TryAdd("ember_blade", 1, stackable: false));
        Assert.False(inventory.TryAdd("coins", 5, stackable: true));
    }

    [Fact]
    public void GridInventory_StackablesShareOneSlot()
    {
        var inventory = new GridInventory();

        inventory.TryAdd("coins", 1000, stackable: true);
        inventory.TryAdd("coins", 500, stackable: true);

        Assert.Equal(1500, inventory.CountOf("coins"));
        Assert.Equal(1, inventory.UsedSlots);
    }

    [Fact]
    public void GridInventory_RemovesAcrossSlots()
    {
        var inventory = new GridInventory();
        inventory.TryAdd("ember_blade", 5, stackable: false);

        Assert.True(inventory.TryRemove("ember_blade", 3));
        Assert.Equal(2, inventory.CountOf("ember_blade"));
        Assert.False(inventory.TryRemove("ember_blade", 99));
    }

    [Fact]
    public void AlchemyTable_ReturnsInvariantFloorValue()
    {
        var alchemy = new AlchemyTable(new Dictionary<string, int> { ["ember_blade"] = 240 });

        Assert.Equal(240, alchemy.Cast("ember_blade"));
        Assert.Equal(240, alchemy.Cast("ember_blade")); // invariant on repeat
        Assert.Equal(0, alchemy.Cast("unknown_item"));
    }

    [Fact]
    public void MarketTax_TakesConfiguredPercentage()
    {
        Assert.Equal(20, MarketTax.FeeFor(1000));
        Assert.Equal(0, MarketTax.FeeFor(0));
    }

    [Fact]
    public void MarketSink_BuysAndPermanentlyDeletesSurplus()
    {
        var sink = new MarketSink();
        sink.RegisterItem("starsteel_greaves", buyPrice: 100, surplusCount: 10);
        sink.CollectTax(450);

        var destroyed = sink.AbsorbSurplus("starsteel_greaves");

        Assert.Equal(4, destroyed);              // 450 / 100 affordable
        Assert.Equal(50, sink.PooledTax);        // remainder pooled
        Assert.Equal(6, sink.SurplusOf("starsteel_greaves"));
        Assert.Equal(4, sink.ItemsDestroyed);

        // Pool can no longer afford another unit.
        Assert.Equal(0, sink.AbsorbSurplus("starsteel_greaves"));
    }
}
