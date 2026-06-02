using System.Collections.Generic;

namespace DawnOfBlade.Inventory;

/// <summary>Account-scoped local bank storage. A server repository can replace persistence later.</summary>
public sealed class BankStorage
{
    private readonly Dictionary<string, int> _items = new();

    public IReadOnlyDictionary<string, int> Items => _items;

    public void Load(IReadOnlyDictionary<string, int> items)
    {
        _items.Clear();
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Key) && item.Value > 0)
            {
                _items[item.Key] = item.Value;
            }
        }
    }

    public bool Deposit(Inventory inventory, string itemId, int quantity = 1)
    {
        if (!inventory.Remove(itemId, quantity))
        {
            return false;
        }

        _items[itemId] = Count(itemId) + quantity;
        return true;
    }

    public bool Withdraw(Inventory inventory, string itemId, int quantity = 1)
    {
        if (quantity <= 0 || Count(itemId) < quantity)
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

        inventory.Add(itemId, quantity);
        return true;
    }

    public int Count(string itemId) => _items.TryGetValue(itemId, out var quantity) ? quantity : 0;
}
