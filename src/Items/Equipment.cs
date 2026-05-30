using System;
using System.Collections.Generic;

namespace DawnOfBlade.Items;

/// <summary>A character's worn equipment, mapping each slot to an item id.</summary>
public sealed class Equipment
{
    private readonly Dictionary<EquipmentSlot, string> _worn = new();

    public IReadOnlyDictionary<EquipmentSlot, string> Worn => _worn;

    public string? ItemInSlot(EquipmentSlot slot) =>
        _worn.TryGetValue(slot, out var itemId) ? itemId : null;

    public void Equip(EquipmentSlot slot, string itemId) => _worn[slot] = itemId;

    public string? Unequip(EquipmentSlot slot)
    {
        if (_worn.TryGetValue(slot, out var itemId))
        {
            _worn.Remove(slot);
            return itemId;
        }

        return null;
    }

    /// <summary>Sums bonuses across all worn items, using <paramref name="lookup"/> to resolve them.</summary>
    public EquipmentBonuses TotalBonuses(Func<string, EquipmentDefinition?> lookup)
    {
        var total = EquipmentBonuses.Zero;
        foreach (var itemId in _worn.Values)
        {
            if (lookup(itemId) is { } definition)
            {
                total = total.Add(new EquipmentBonuses(definition.AttackBonus, definition.StrengthBonus, definition.DefenseBonus));
            }
        }

        return total;
    }
}
