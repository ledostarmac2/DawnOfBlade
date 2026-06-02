using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Data;
using DawnOfBlade.Inventory;
using DawnOfBlade.Items;
using Xunit;

namespace DawnOfBlade.Tests;

public class ExpandedCatalogTests
{
    private static IReadOnlyDictionary<string, ItemDefinition> Items() =>
        DefinitionDatabase.ParseList<ItemDefinition>(TestContent.DataText("items/items.example.json"))
            .ToDictionary(item => item.Id);

    private static IReadOnlyDictionary<string, EquipmentDefinition> Equipment() =>
        DefinitionDatabase.ParseList<EquipmentDefinition>(TestContent.DataText("equipment/equipment.example.json"))
            .ToDictionary(item => item.ItemId);

    [Fact]
    public void Catalog_ContainsExpandedResourcesFoodAndAmmunition()
    {
        var items = Items();

        Assert.True(items.Count >= 70);
        AssertContains(items, "logs", "oak_logs", "willow_logs");
        AssertContains(items, "copper_ore", "tin_ore", "iron_ore", "coal", "silver_ore", "gold_ore");
        AssertContains(items, "raw_shrimp", "raw_trout", "raw_salmon", "raw_lobster");
        AssertContains(items, "bread", "cooked_shrimp", "cooked_trout", "cooked_salmon", "cooked_lobster");
        AssertContains(items, "bronze_arrows", "iron_arrows", "steel_arrows");
        Assert.All(new[] { "bronze_arrows", "iron_arrows", "steel_arrows" }, id => Assert.True(items[id].Stackable));
    }

    [Fact]
    public void Equipment_ContainsWeaponAndShieldProgressions()
    {
        var equipment = Equipment();

        AssertContains(equipment, "bronze_sword", "iron_sword", "steel_sword");
        AssertContains(equipment, "shortbow", "oak_shortbow", "willow_longbow");
        AssertContains(equipment, "elemental_staff", "ember_staff", "tide_staff");
        AssertContains(equipment, "bronze_shield", "iron_shield", "steel_shield");
    }

    [Theory]
    [InlineData("bronze", "helm", "chestplate", "legplates", "gloves", "boots")]
    [InlineData("iron", "helm", "chestplate", "legplates", "gloves", "boots")]
    [InlineData("steel", "helm", "chestplate", "legplates", "gloves", "boots")]
    [InlineData("leather", "cowl", "jerkin", "chaps", "gloves", "boots")]
    [InlineData("mind", "hood", "robes", "leggings", "gloves", "boots")]
    public void Equipment_ContainsCompleteArmorSets(
        string prefix,
        string head,
        string body,
        string legs,
        string hands,
        string feet)
    {
        var equipment = Equipment();

        Assert.Equal("Head", equipment[$"{prefix}_{head}"].Slot);
        Assert.Equal("Body", equipment[$"{prefix}_{body}"].Slot);
        Assert.Equal("Legs", equipment[$"{prefix}_{legs}"].Slot);
        Assert.Equal("Hands", equipment[$"{prefix}_{hands}"].Slot);
        Assert.Equal("Feet", equipment[$"{prefix}_{feet}"].Slot);
    }

    private static void AssertContains<T>(IReadOnlyDictionary<string, T> values, params string[] ids)
    {
        Assert.All(ids, id => Assert.True(values.ContainsKey(id), $"Missing catalog entry: {id}"));
    }
}
