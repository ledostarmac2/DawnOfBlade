using System.Collections.Generic;
using System.Linq;

namespace DawnOfBlade.GameSystems;

/// <summary>
/// The centralized limit-order market board (Parts 5.2 + 19.1). Sellers escrow goods into a passive
/// listing; buyers pass a gold-validation gate, escrow their maximum spend, then cross the book from
/// the lowest sell price upward with partial fills. Trades execute at the resting sell price and the
/// buyer is refunded the price spread. Filled goods and gold land in per-owner collection depots, and
/// every move is dispatched to the optional <see cref="TransactionLogger"/> audit bus. Pure C#:
/// matching is deterministic given the same order sequence, so it is server-reproducible and testable.
/// </summary>
public sealed class MarketEngine
{
    private readonly List<MarketOrder> _sells = new();
    private readonly List<MarketOrder> _buys = new();
    private readonly Dictionary<string, Dictionary<int, int>> _depotItems = new();
    private readonly Dictionary<string, long> _depotGold = new();
    private readonly Dictionary<string, long> _escrowGold = new();
    private readonly TransactionLogger? _log;
    private long _nextOrderId = 1;

    public MarketEngine(TransactionLogger? log = null) => _log = log;

    public int OpenSellCount => _sells.Count(o => !o.IsFilled);
    public int OpenBuyCount => _buys.Count(o => !o.IsFilled);

    public long DepotGold(string ownerId) => _depotGold.TryGetValue(ownerId, out var gold) ? gold : 0;
    public long EscrowGold(string ownerId) => _escrowGold.TryGetValue(ownerId, out var gold) ? gold : 0;

    public int DepotItemCount(string ownerId, int itemId) =>
        _depotItems.TryGetValue(ownerId, out var items) && items.TryGetValue(itemId, out var qty) ? qty : 0;

    /// <summary>List goods for sale at a fixed unit price; immediately crosses any matching buy orders.</summary>
    public OrderResult PlaceSellOrder(string sellerId, int itemId, int quantity, long unitPrice, long timestamp = 0)
    {
        if (quantity <= 0 || unitPrice <= 0)
        {
            return new OrderResult(false, "Quantity and price must be positive.", 0, 0, 0, System.Array.Empty<MarketFill>());
        }

        var order = new MarketOrder(_nextOrderId++, OrderSide.Sell, sellerId, itemId, unitPrice, quantity);
        _sells.Add(order);
        var fills = CrossBook(timestamp);
        return Result(order, fills);
    }

    /// <summary>
    /// Submit a buy request up to <paramref name="maxUnitPrice"/>. The gate verifies the buyer can
    /// cover price × quantity; that gold is escrowed, then the order crosses the book.
    /// </summary>
    public OrderResult PlaceBuyOrder(string buyerId, int itemId, int quantity, long maxUnitPrice, long buyerGold, long timestamp = 0)
    {
        if (quantity <= 0 || maxUnitPrice <= 0)
        {
            return new OrderResult(false, "Quantity and price must be positive.", 0, 0, 0, System.Array.Empty<MarketFill>());
        }

        var required = maxUnitPrice * quantity;
        if (buyerGold < required)
        {
            return new OrderResult(false, "Insufficient gold for this order.", 0, 0, 0, System.Array.Empty<MarketFill>());
        }

        _escrowGold[buyerId] = EscrowGold(buyerId) + required;
        var order = new MarketOrder(_nextOrderId++, OrderSide.Buy, buyerId, itemId, maxUnitPrice, quantity);
        _buys.Add(order);
        var fills = CrossBook(timestamp);
        return Result(order, fills);
    }

    private static OrderResult Result(MarketOrder order, IReadOnlyList<MarketFill> fills)
    {
        var filled = fills.Where(f => Owns(order, f)).Sum(f => f.Quantity);
        return new OrderResult(true, "Accepted.", order.OrderId, filled, order.RemainingQuantity, fills);
    }

    private static bool Owns(MarketOrder order, MarketFill fill) =>
        order.Side == OrderSide.Buy ? fill.BuyerId == order.OwnerId : fill.SellerId == order.OwnerId;

    private IReadOnlyList<MarketFill> CrossBook(long timestamp)
    {
        var fills = new List<MarketFill>();

        while (true)
        {
            var bestSell = _sells.Where(o => !o.IsFilled)
                .OrderBy(o => o.UnitPrice).ThenBy(o => o.OrderId).FirstOrDefault();
            var bestBuy = _buys.Where(o => !o.IsFilled)
                .OrderByDescending(o => o.UnitPrice).ThenBy(o => o.OrderId).FirstOrDefault();

            if (bestSell is null || bestBuy is null || bestSell.ItemId != bestBuy.ItemId || bestSell.UnitPrice > bestBuy.UnitPrice)
            {
                break;
            }

            var quantity = System.Math.Min(bestSell.RemainingQuantity, bestBuy.RemainingQuantity);
            var price = bestSell.UnitPrice; // resting sell price favours the buyer
            var buyer = bestBuy.OwnerId;
            var seller = bestSell.OwnerId;

            CreditItems(buyer, bestSell.ItemId, quantity);
            _depotGold[seller] = DepotGold(seller) + quantity * price;
            _escrowGold[buyer] = EscrowGold(buyer) - quantity * bestBuy.UnitPrice;
            var refund = quantity * (bestBuy.UnitPrice - price);
            if (refund > 0)
            {
                _depotGold[buyer] = DepotGold(buyer) + refund;
            }

            bestSell.RemainingQuantity -= quantity;
            bestBuy.RemainingQuantity -= quantity;

            fills.Add(new MarketFill(buyer, seller, bestSell.ItemId, quantity, price));
            _log?.Log(new TransactionRecord(
                timestamp, buyer, seller, TransactionAction.MarketBuy, bestSell.ItemId, quantity,
                $"market:sell:{bestSell.OrderId}", $"depot:{buyer}"));
        }

        _sells.RemoveAll(o => o.IsFilled);
        _buys.RemoveAll(o => o.IsFilled);
        return fills;
    }

    private void CreditItems(string ownerId, int itemId, int quantity)
    {
        if (!_depotItems.TryGetValue(ownerId, out var items))
        {
            items = new Dictionary<int, int>();
            _depotItems[ownerId] = items;
        }

        items[itemId] = (items.TryGetValue(itemId, out var existing) ? existing : 0) + quantity;
    }
}
