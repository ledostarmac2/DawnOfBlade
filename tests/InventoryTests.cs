using DawnOfBlade.Inventory;
using Xunit;

namespace DawnOfBlade.Tests;

public class InventoryTests
{
    [Fact]
    public void Add_AccumulatesQuantity()
    {
        var inventory = new Inventory.Inventory();
        inventory.Add("sunleaf", 2);
        inventory.Add("sunleaf");

        Assert.Equal(3, inventory.Count("sunleaf"));
    }

    [Fact]
    public void Add_IgnoresInvalidInput()
    {
        var inventory = new Inventory.Inventory();
        inventory.Add("", 5);
        inventory.Add("sunleaf", 0);
        inventory.Add("sunleaf", -3);

        Assert.Equal(0, inventory.Count("sunleaf"));
    }

    [Fact]
    public void Remove_FailsWhenNotEnough()
    {
        var inventory = new Inventory.Inventory();
        inventory.Add("sunleaf", 1);

        Assert.False(inventory.Remove("sunleaf", 2));
        Assert.Equal(1, inventory.Count("sunleaf"));
    }

    [Fact]
    public void Remove_RemovesKeyWhenZero()
    {
        var inventory = new Inventory.Inventory();
        inventory.Add("sunleaf", 2);

        Assert.True(inventory.Remove("sunleaf", 2));
        Assert.Equal(0, inventory.Count("sunleaf"));
        Assert.DoesNotContain("sunleaf", inventory.Items.Keys);
    }
}
