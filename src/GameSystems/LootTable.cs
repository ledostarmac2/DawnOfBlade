using System.Collections.Generic;

namespace DawnOfBlade.GameSystems;

/// <summary>An item id + quantity produced by a drop or harvest.</summary>
public readonly record struct LootDrop(int ItemId, int Quantity);

/// <summary>A weighted entry in the standard-drop pool (weights scale out of 10,000).</summary>
public readonly record struct WeightedDrop(int ItemId, int Quantity, int Weight);

/// <summary>An independent rare-drop check evaluated as a 1-in-<see cref="OneInChance"/> roll.</summary>
public readonly record struct RareDrop(int ItemId, int Quantity, int OneInChance);

/// <summary>
/// Multi-tier loot table (Part 18.1). Guaranteed drops always fire; exactly one standard drop is
/// selected from the weighted pool (out of 10,000); each rare drop is an independent fractional roll.
/// </summary>
public sealed class LootTable
{
    public const int StandardPool = 10_000;

    public IReadOnlyList<LootDrop> GuaranteedDrops { get; }
    public IReadOnlyList<WeightedDrop> StandardDrops { get; }
    public IReadOnlyList<RareDrop> RareDrops { get; }

    public LootTable(
        IReadOnlyList<LootDrop>? guaranteed = null,
        IReadOnlyList<WeightedDrop>? standard = null,
        IReadOnlyList<RareDrop>? rare = null)
    {
        GuaranteedDrops = guaranteed ?? System.Array.Empty<LootDrop>();
        StandardDrops = standard ?? System.Array.Empty<WeightedDrop>();
        RareDrops = rare ?? System.Array.Empty<RareDrop>();
    }
}
