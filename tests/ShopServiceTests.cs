using DawnOfBlade.Shops;
using Xunit;
using InventoryBag = DawnOfBlade.Inventory.Inventory;

namespace DawnOfBlade.Tests;

public class ShopServiceTests
{
    private static ShopStock BuildShop() => new(new ShopDefinition(
        "village_general",
        "Crossroads Supplies",
        new[]
        {
            new ShopStockItem("bronze_dagger", 2, 25),
            new ShopStockItem("sunleaf", 10, 4),
        }));

    [Fact]
    public void Buy_DeductsCoinsAddsItemAndReducesStock()
    {
        var shop = BuildShop();
        var inventory = new InventoryBag();
        inventory.Add("coins", 30);

        var (ok, _) = ShopService.Buy(shop, "bronze_dagger", inventory);

        Assert.True(ok);
        Assert.Equal(1, inventory.Count("bronze_dagger"));
        Assert.Equal(5, inventory.Count("coins"));
        Assert.Equal(1, shop.QuantityOf("bronze_dagger"));
    }

    [Fact]
    public void Buy_FailsWithoutEnoughCoins()
    {
        var shop = BuildShop();
        var inventory = new InventoryBag();
        inventory.Add("coins", 10);

        var (ok, _) = ShopService.Buy(shop, "bronze_dagger", inventory);

        Assert.False(ok);
        Assert.Equal(0, inventory.Count("bronze_dagger"));
        Assert.Equal(10, inventory.Count("coins"));
    }

    [Fact]
    public void Sell_AddsHalfPriceCoinsAndRestocks()
    {
        var shop = BuildShop();
        var inventory = new InventoryBag();
        inventory.Add("bronze_dagger");

        var (ok, _) = ShopService.Sell(shop, "bronze_dagger", inventory);

        Assert.True(ok);
        Assert.Equal(0, inventory.Count("bronze_dagger"));
        Assert.Equal(12, inventory.Count("coins"));
        Assert.Equal(3, shop.QuantityOf("bronze_dagger"));
    }

    [Fact]
    public void Buy_RejectsItemsNotStocked()
    {
        var shop = BuildShop();
        var inventory = new InventoryBag();
        inventory.Add("coins", 100);

        var (ok, _) = ShopService.Buy(shop, "iron_shortblade", inventory);

        Assert.False(ok);
    }
}
