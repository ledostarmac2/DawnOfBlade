using System;
using InventoryBag = DawnOfBlade.Inventory.Inventory;

namespace DawnOfBlade.Shops;

/// <summary>
/// Buy/sell logic between a player <see cref="InventoryBag"/> and a <see cref="ShopStock"/>.
/// Currency is the inventory item <c>coins</c>. Specialty shops only trade items they stock;
/// sell price is half the buy price.
/// </summary>
public static class ShopService
{
    public const string Currency = "coins";

    public static (bool Ok, string Message) Buy(ShopStock shop, string itemId, InventoryBag inventory)
    {
        if (!shop.Stocks(itemId))
        {
            return (false, "The shop doesn't sell that.");
        }

        if (shop.QuantityOf(itemId) <= 0)
        {
            return (false, "Out of stock.");
        }

        var price = shop.PriceOf(itemId);
        if (inventory.Count(Currency) < price)
        {
            return (false, "Not enough coins.");
        }

        inventory.Remove(Currency, price);
        inventory.Add(itemId);
        shop.Reduce(itemId, 1);
        return (true, $"Bought 1 for {price} coins.");
    }

    public static (bool Ok, string Message) Sell(ShopStock shop, string itemId, InventoryBag inventory)
    {
        if (!shop.Stocks(itemId))
        {
            return (false, "The shop won't buy that.");
        }

        if (inventory.Count(itemId) <= 0)
        {
            return (false, "You have none to sell.");
        }

        var price = Math.Max(1, shop.PriceOf(itemId) / 2);
        inventory.Remove(itemId, 1);
        inventory.Add(Currency, price);
        shop.Increase(itemId, 1);
        return (true, $"Sold 1 for {price} coins.");
    }
}
