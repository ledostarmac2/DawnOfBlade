using System.Collections.Generic;
using DawnOfBlade.Combat;

namespace DawnOfBlade.GameSystems;

/// <summary>
/// Rolls a <see cref="LootTable"/> following the Part 18.1 pipeline: emit every guaranteed drop,
/// select at most one standard drop by cumulative weight over a 1..10,000 roll, then fire each rare
/// drop as an independent 1-in-N check. Randomness is injected (<see cref="IRandomSource"/>) so drops
/// are deterministic and server-reproducible in tests.
/// </summary>
public static class LootRoller
{
    public static IReadOnlyList<LootDrop> Roll(LootTable table, IRandomSource random)
    {
        System.ArgumentNullException.ThrowIfNull(table);
        System.ArgumentNullException.ThrowIfNull(random);

        var drops = new List<LootDrop>();

        foreach (var guaranteed in table.GuaranteedDrops)
        {
            drops.Add(guaranteed);
        }

        if (table.StandardDrops.Count > 0)
        {
            var roll = random.Next(1, LootTable.StandardPool + 1); // 1..10000
            var cumulative = 0;
            foreach (var entry in table.StandardDrops)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative)
                {
                    drops.Add(new LootDrop(entry.ItemId, entry.Quantity));
                    break;
                }
            }
            // If the roll exceeds the summed weights, no standard item drops (the "nothing" slice).
        }

        foreach (var rare in table.RareDrops)
        {
            if (rare.OneInChance > 0 && random.Next(rare.OneInChance) == 0)
            {
                drops.Add(new LootDrop(rare.ItemId, rare.Quantity));
            }
        }

        return drops;
    }
}
