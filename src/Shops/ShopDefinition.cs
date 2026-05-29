using System.Collections.Generic;

namespace DawnOfBlade.Shops;

public sealed record ShopStockItem(
    string ItemId,
    int Quantity,
    int Price
);

public sealed record ShopDefinition(
    string Id,
    string DisplayName,
    IReadOnlyList<ShopStockItem> Stock
);

