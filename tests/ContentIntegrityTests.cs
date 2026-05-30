using System.Linq;
using DawnOfBlade.Data;
using DawnOfBlade.Inventory;
using DawnOfBlade.Items;
using DawnOfBlade.Shops;
using Xunit;

namespace DawnOfBlade.Tests;

/// <summary>
/// Cross-file checks: equipment and shop content must reference real items and use valid slots,
/// so the running game never points at a missing definition.
/// </summary>
public class ContentIntegrityTests
{
    private static System.Collections.Generic.HashSet<string> ItemIds() =>
        DefinitionDatabase.ParseList<ItemDefinition>(TestContent.DataText("items/items.example.json"))
            .Select(i => i.Id)
            .ToHashSet();

    [Fact]
    public void Equipment_ReferencesRealItemsWithValidSlots()
    {
        var items = ItemIds();
        var equipment = DefinitionDatabase.ParseList<EquipmentDefinition>(TestContent.DataText("equipment/equipment.example.json"));

        Assert.NotEmpty(equipment);
        Assert.All(equipment, e => Assert.Contains(e.ItemId, items));
        Assert.All(equipment, e => Assert.True(
            System.Enum.TryParse<EquipmentSlot>(e.Slot, ignoreCase: true, out _),
            $"Invalid equipment slot: {e.Slot}"));
    }

    [Fact]
    public void Shops_StockRealItemsWithSanePrices()
    {
        var items = ItemIds();
        var shops = DefinitionDatabase.ParseList<ShopDefinition>(TestContent.DataText("shops/shops.example.json"));

        Assert.NotEmpty(shops);
        Assert.All(shops, shop => Assert.All(shop.Stock, entry =>
        {
            Assert.Contains(entry.ItemId, items);
            Assert.True(entry.Price > 0);
            Assert.True(entry.Quantity >= 0);
        }));
    }

    [Fact]
    public void Currency_ItemExists()
    {
        Assert.Contains(ShopService.Currency, ItemIds());
    }
}
