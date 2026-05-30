using System;

namespace DawnOfBlade.Combat;

/// <summary>
/// Abstraction over randomness so combat (and other systems) can be exercised deterministically
/// in tests by supplying a scripted source.
/// </summary>
public interface IRandomSource
{
    double NextDouble();
    int Next(int maxExclusive);
    int Next(int minInclusive, int maxExclusive);
}

/// <summary>Default <see cref="IRandomSource"/> backed by <see cref="System.Random"/>.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource() => _random = new Random();

    public SystemRandomSource(int seed) => _random = new Random(seed);

    public double NextDouble() => _random.NextDouble();

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
