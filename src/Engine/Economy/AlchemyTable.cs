using System.Collections.Generic;

namespace DawnOfBlade.Engine.Economy;

/// <summary>
/// The <em>Aurum Rite</em> — an original Dawn of Blade conversion spell that instantly turns any
/// data-driven item id into a hardcoded, invariant coin amount. Because the coin payout never
/// changes, it establishes an absolute market floor price for every convertible item.
/// </summary>
public sealed class AlchemyTable
{
    private readonly Dictionary<string, int> _coinValues;

    public AlchemyTable(IDictionary<string, int>? seed = null) =>
        _coinValues = seed is null ? new Dictionary<string, int>() : new Dictionary<string, int>(seed);

    public void SetValue(string itemId, int coins) => _coinValues[itemId] = coins;

    public bool HasValue(string itemId) => _coinValues.ContainsKey(itemId);

    public int ValueOf(string itemId) => _coinValues.TryGetValue(itemId, out var value) ? value : 0;

    /// <summary>Casts the Aurum Rite on an item id, returning its invariant coin floor.</summary>
    public int Cast(string itemId) => ValueOf(itemId);
}
