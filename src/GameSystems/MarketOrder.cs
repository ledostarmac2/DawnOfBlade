namespace DawnOfBlade.GameSystems;

public enum OrderSide
{
    Buy,
    Sell,
}

/// <summary>A resting limit order on the market board. <see cref="RemainingQuantity"/> shrinks as it fills.</summary>
public sealed class MarketOrder
{
    public MarketOrder(long orderId, OrderSide side, string ownerId, int itemId, long unitPrice, int quantity)
    {
        OrderId = orderId;
        Side = side;
        OwnerId = ownerId;
        ItemId = itemId;
        UnitPrice = unitPrice;
        RemainingQuantity = quantity;
    }

    public long OrderId { get; }
    public OrderSide Side { get; }
    public string OwnerId { get; }
    public int ItemId { get; }
    public long UnitPrice { get; }
    public int RemainingQuantity { get; internal set; }

    public bool IsFilled => RemainingQuantity <= 0;
}

/// <summary>A single executed trade between a buy and a sell order, settled at the resting sell price.</summary>
public readonly record struct MarketFill(string BuyerId, string SellerId, int ItemId, int Quantity, long UnitPrice);

/// <summary>Outcome of submitting an order: whether it was accepted, what it filled, and what stays resting.</summary>
public readonly record struct OrderResult(
    bool Accepted,
    string Message,
    long OrderId,
    int FilledQuantity,
    int RestingQuantity,
    System.Collections.Generic.IReadOnlyList<MarketFill> Fills);
