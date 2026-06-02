using System.Collections.Generic;
using System.Linq;

namespace DawnOfBlade.World.Grid;

/// <summary>Filters entity updates to the chunk window relevant to a player.</summary>
public sealed class ChunkInterestManager
{
    public ChunkInterestManager(int chunkSize = ChunkCoordinate.DefaultSize, int windowRadius = 1)
    {
        if (chunkSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (windowRadius < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(windowRadius));
        }

        ChunkSize = chunkSize;
        WindowRadius = windowRadius;
    }

    public int ChunkSize { get; }
    public int WindowRadius { get; }

    public bool IsRelevant(GridCoordinate observer, GridCoordinate subject) =>
        subject.ToChunk(ChunkSize).IsWithinWindow(observer.ToChunk(ChunkSize), WindowRadius);

    public IEnumerable<T> FilterRelevant<T>(
        GridCoordinate observer,
        IEnumerable<T> subjects,
        System.Func<T, GridCoordinate> coordinateSelector) =>
        subjects.Where(subject => IsRelevant(observer, coordinateSelector(subject)));
}
