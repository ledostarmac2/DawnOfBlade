using System.Collections.Generic;
using DawnOfBlade.Items;
using Xunit;

namespace DawnOfBlade.Tests;

public class EquipmentTests
{
    private static EquipmentDefinition? Lookup(IReadOnlyDictionary<string, EquipmentDefinition> defs, string id) =>
        defs.TryGetValue(id, out var def) ? def : null;

    [Fact]
    public void TotalBonuses_SumsWornItems()
    {
        var defs = new Dictionary<string, EquipmentDefinition>
        {
            ["bronze_dagger"] = new("bronze_dagger", "Weapon", 4, 3, 0),
            ["oak_buckler"] = new("oak_buckler", "Shield", 0, 0, 5),
        };

        var equipment = new Equipment();
        equipment.Equip(EquipmentSlot.Weapon, "bronze_dagger");
        equipment.Equip(EquipmentSlot.Shield, "oak_buckler");

        var bonuses = equipment.TotalBonuses(id => Lookup(defs, id));

        Assert.Equal(4, bonuses.Attack);
        Assert.Equal(3, bonuses.Strength);
        Assert.Equal(5, bonuses.Defense);
    }

    [Fact]
    public void Equip_ReplacesSlotAndUnequipReturnsItem()
    {
        var equipment = new Equipment();
        equipment.Equip(EquipmentSlot.Weapon, "bronze_dagger");
        equipment.Equip(EquipmentSlot.Weapon, "iron_shortblade");

        Assert.Equal("iron_shortblade", equipment.ItemInSlot(EquipmentSlot.Weapon));

        var removed = equipment.Unequip(EquipmentSlot.Weapon);
        Assert.Equal("iron_shortblade", removed);
        Assert.Null(equipment.ItemInSlot(EquipmentSlot.Weapon));
    }
}
