using System.Collections.Generic;
using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems;

/// <summary>
/// Engine-independent resource respawn manager (Part 18.2). It owns a set of anchor tiles that can
/// hold a node (ore vein, tree, …). Depleting a tile caches its coordinate with a tick timestamp; on
/// each global tick the pool returns the tiles whose timestamp has elapsed so the host can re-instance
/// a mesh and re-block the tile. No Godot types are referenced, so the regrow math is fully testable.
/// </summary>
public sealed class ResourceSpawnerPool
{
    private readonly HashSet<GridCoordinate> _anchors = new();
    private readonly HashSet<GridCoordinate> _active = new();
    private readonly Dictionary<GridCoordinate, long> _respawnAt = new();

    public ResourceSpawnerPool(IEnumerable<GridCoordinate> anchors)
    {
        System.ArgumentNullException.ThrowIfNull(anchors);
        foreach (var anchor in anchors)
        {
            _anchors.Add(anchor);
            _active.Add(anchor);
        }
    }

    public int ActiveCount => _active.Count;
    public int DepletedCount => _respawnAt.Count;
    public bool IsActive(GridCoordinate coordinate) => _active.Contains(coordinate);

    /// <summary>Mark a tile harvested. It goes dormant and is queued to regrow after the delay.</summary>
    public void Deplete(GridCoordinate coordinate, long currentTick, long respawnDelayTicks)
    {
        if (!_anchors.Contains(coordinate) || !_active.Remove(coordinate))
        {
            return;
        }

        var delay = respawnDelayTicks < 0 ? 0 : respawnDelayTicks;
        _respawnAt[coordinate] = currentTick + delay;
    }

    /// <summary>
    /// Advance the pool to <paramref name="currentTick"/>, reactivating every tile whose regrow
    /// timestamp has elapsed. Returns the coordinates that just respawned (for re-instancing).
    /// </summary>
    public IReadOnlyList<GridCoordinate> Tick(long currentTick)
    {
        List<GridCoordinate>? respawned = null;
        foreach (var pair in _respawnAt)
        {
            if (currentTick >= pair.Value)
            {
                (respawned ??= new List<GridCoordinate>()).Add(pair.Key);
            }
        }

        if (respawned is null)
        {
            return System.Array.Empty<GridCoordinate>();
        }

        foreach (var coordinate in respawned)
        {
            _respawnAt.Remove(coordinate);
            _active.Add(coordinate);
        }

        return respawned;
    }
}
