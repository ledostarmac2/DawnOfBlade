using System.Collections.Generic;
using System.Linq;

namespace DawnOfBlade.Engine.Economy;

/// <summary>
/// A fixed 28-slot inventory grid. Gear, supplies, and gathered resources all consume slots with
/// identical friction: stackable items share one slot; non-stackable items each take a slot.
/// Slot reordering (<see cref="Swap"/>) is a Phase 0 interface operation.
/// </summary>
public sealed class GridInventory
{
    public const int Capacity = 28;

    private readonly (string Id, int Quantity)?[] _slots = new (string, int)?[Capacity];

    public int UsedSlots => _slots.Count(slot => slot.HasValue);

    public int FreeSlots => Capacity - UsedSlots;

    public IReadOnlyList<(string Id, int Quantity)?> Slots => _slots;

    public int CountOf(string itemId) =>
        _slots.Where(s => s.HasValue && s.Value.Id == itemId).Sum(s => s!.Value.Quantity);

    /// <summary>Adds items, all-or-nothing. Returns false if there isn't room.</summary>
    public bool TryAdd(string itemId, int quantity, bool stackable)
    {
        if (quantity <= 0)
        {
            return false;
        }

        if (stackable)
        {
            var index = FindSlot(itemId);
            if (index >= 0)
            {
                _slots[index] = (itemId, _slots[index]!.Value.Quantity + quantity);
                return true;
            }

            var free = FirstFreeSlot();
            if (free < 0)
            {
                return false;
            }

            _slots[free] = (itemId, quantity);
            return true;
        }

        if (FreeSlots < quantity)
        {
            return false;
        }

        for (var placed = 0; placed < quantity; placed++)
        {
            _slots[FirstFreeSlot()] = (itemId, 1);
        }

        return true;
    }

    /// <summary>Removes items across slots, all-or-nothing. Returns false if not enough are present.</summary>
    public bool TryRemove(string itemId, int quantity)
    {
        if (quantity <= 0 || CountOf(itemId) < quantity)
        {
            return false;
        }

        var remaining = quantity;
        for (var i = 0; i < Capacity && remaining > 0; i++)
        {
            if (_slots[i] is not { } slot || slot.Id != itemId)
            {
                continue;
            }

            var take = System.Math.Min(remaining, slot.Quantity);
            var left = slot.Quantity - take;
            _slots[i] = left > 0 ? (itemId, left) : null;
            remaining -= take;
        }

        return true;
    }

    public void Swap(int slotA, int slotB) => (_slots[slotA], _slots[slotB]) = (_slots[slotB], _slots[slotA]);

    private int FindSlot(string itemId)
    {
        for (var i = 0; i < Capacity; i++)
        {
            if (_slots[i] is { } slot && slot.Id == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FirstFreeSlot()
    {
        for (var i = 0; i < Capacity; i++)
        {
            if (!_slots[i].HasValue)
            {
                return i;
            }
        }

        return -1;
    }
}
