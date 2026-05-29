using System.Collections.Generic;

namespace DawnOfBlade.Inventory;

public sealed class Inventory
{
    private readonly Dictionary<string, int> _items = new();

    public IReadOnlyDictionary<string, int> Items => _items;

    public int Count(string itemId)
    {
        return _items.TryGetValue(itemId, out var count) ? count : 0;
    }

    public void Add(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        _items[itemId] = Count(itemId) + quantity;
    }

    public bool Remove(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0 || Count(itemId) < quantity)
        {
            return false;
        }

        var remaining = Count(itemId) - quantity;
        if (remaining == 0)
        {
            _items.Remove(itemId);
        }
        else
        {
            _items[itemId] = remaining;
        }

        return true;
    }
}

