using System;
using System.Collections.Generic;

namespace DawnOfBlade.Shops;

/// <summary>
/// Runtime, mutable stock for a shop, built from its immutable <see cref="ShopDefinition"/>.
/// Tracks remaining quantity and price per item.
/// </summary>
public sealed class ShopStock
{
    private readonly Dictionary<string, int> _quantity = new();
    private readonly Dictionary<string, int> _price = new();
    private readonly List<string> _order = new();

    public ShopStock(ShopDefinition definition)
    {
        Id = definition.Id;
        DisplayName = definition.DisplayName;

        foreach (var entry in definition.Stock)
        {
            _quantity[entry.ItemId] = entry.Quantity;
            _price[entry.ItemId] = entry.Price;
            _order.Add(entry.ItemId);
        }
    }

    public string Id { get; }
    public string DisplayName { get; }

    public IReadOnlyList<string> ItemIds => _order;

    public bool Stocks(string itemId) => _price.ContainsKey(itemId);

    public int QuantityOf(string itemId) => _quantity.TryGetValue(itemId, out var value) ? value : 0;

    public int PriceOf(string itemId) => _price.TryGetValue(itemId, out var value) ? value : 0;

    public void Reduce(string itemId, int amount)
    {
        if (_quantity.ContainsKey(itemId))
        {
            _quantity[itemId] = Math.Max(0, _quantity[itemId] - amount);
        }
    }

    public void Increase(string itemId, int amount)
    {
        if (_quantity.ContainsKey(itemId))
        {
            _quantity[itemId] += amount;
        }
    }
}
