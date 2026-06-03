using System;
using System.Collections.Generic;

namespace DawnOfBlade.Engine.Economy;

/// <summary>The global market transaction fee applied to a sale.</summary>
public static class MarketTax
{
    /// <summary>Default 2% transaction fee.</summary>
    public const double DefaultRate = 0.02;

    public static long FeeFor(long salePrice, double rate = DefaultRate) =>
        (long)Math.Floor(Math.Max(0, salePrice) * Math.Clamp(rate, 0.0, 1.0));
}

/// <summary>
/// A deflationary currency sink. Transaction taxes pool here; the sink then spends the pool to buy
/// surplus high-tier item ids out of the market pool and permanently delete them, countering
/// long-term asset inflation. Money in (tax) and assets out (deleted) never re-enter circulation.
/// </summary>
public sealed class MarketSink
{
    private readonly Dictionary<string, int> _surplus = new();
    private readonly Dictionary<string, int> _buyPrice = new();

    public long PooledTax { get; private set; }

    public long ItemsDestroyed { get; private set; }

    /// <summary>Registers a high-tier item the sink will absorb, with its market buy price and surplus.</summary>
    public void RegisterItem(string itemId, int buyPrice, int surplusCount)
    {
        _buyPrice[itemId] = buyPrice;
        _surplus[itemId] = surplusCount;
    }

    public int SurplusOf(string itemId) => _surplus.TryGetValue(itemId, out var value) ? value : 0;

    /// <summary>Adds collected transaction tax to the pool.</summary>
    public void CollectTax(long amount)
    {
        if (amount > 0)
        {
            PooledTax += amount;
        }
    }

    /// <summary>
    /// Spends the pool to buy and permanently delete as many surplus units of an item id as the
    /// pool can afford. Returns the number destroyed.
    /// </summary>
    public int AbsorbSurplus(string itemId)
    {
        if (!_buyPrice.TryGetValue(itemId, out var price) || price <= 0)
        {
            return 0;
        }

        if (!_surplus.TryGetValue(itemId, out var available) || available <= 0)
        {
            return 0;
        }

        var affordable = (int)(PooledTax / price);
        var destroyed = Math.Min(affordable, available);
        if (destroyed <= 0)
        {
            return 0;
        }

        PooledTax -= (long)destroyed * price;
        _surplus[itemId] = available - destroyed;
        ItemsDestroyed += destroyed;
        return destroyed;
    }
}
