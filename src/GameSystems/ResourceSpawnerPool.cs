using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.World.Grid;

namespace DawnOfBlade.GameSystems;

public sealed record ResourceSpawnAnchor(string NodeId, GridCoordinate Coordinate);

/// <summary>
/// Engine-independent resource respawn manager. Stable node ids are authoritative; coordinates
/// remain placement metadata and a compatibility lookup for world adapters.
/// </summary>
public sealed class ResourceSpawnerPool
{
    private readonly Dictionary<string, ResourceSpawnAnchor> _anchorsById = new(System.StringComparer.Ordinal);
    private readonly Dictionary<GridCoordinate, ResourceSpawnAnchor> _anchorsByCoordinate = new();
    private readonly HashSet<string> _active = new(System.StringComparer.Ordinal);
    private readonly Dictionary<string, long> _respawnAt = new(System.StringComparer.Ordinal);

    public ResourceSpawnerPool(IEnumerable<ResourceSpawnAnchor> anchors)
    {
        System.ArgumentNullException.ThrowIfNull(anchors);
        foreach (var anchor in anchors)
        {
            if (string.IsNullOrWhiteSpace(anchor.NodeId))
            {
                throw new System.ArgumentException("Resource node ids must not be empty.", nameof(anchors));
            }

            if (!_anchorsById.TryAdd(anchor.NodeId, anchor))
            {
                throw new System.ArgumentException($"Duplicate resource node id: {anchor.NodeId}.", nameof(anchors));
            }

            if (!_anchorsByCoordinate.TryAdd(anchor.Coordinate, anchor))
            {
                throw new System.ArgumentException($"Duplicate resource coordinate: {anchor.Coordinate}.", nameof(anchors));
            }

            _active.Add(anchor.NodeId);
        }
    }

    public ResourceSpawnerPool(IEnumerable<GridCoordinate> anchors)
        : this(anchors.Select((coordinate, index) => new ResourceSpawnAnchor($"legacy_resource_{index:00}", coordinate)))
    {
    }

    public int ActiveCount => _active.Count;
    public int DepletedCount => _respawnAt.Count;
    public bool IsActive(string nodeId) => _active.Contains(nodeId);
    public bool IsActive(GridCoordinate coordinate) =>
        TryGetByCoordinate(coordinate, out var anchor) && IsActive(anchor.NodeId);

    public bool TryGetAnchor(string nodeId, out ResourceSpawnAnchor anchor) =>
        _anchorsById.TryGetValue(nodeId, out anchor!);

    public bool TryGetByCoordinate(GridCoordinate coordinate, out ResourceSpawnAnchor anchor) =>
        _anchorsByCoordinate.TryGetValue(coordinate, out anchor!);

    public void Deplete(string nodeId, long currentTick, long respawnDelayTicks)
    {
        if (!_anchorsById.ContainsKey(nodeId) || !_active.Remove(nodeId))
        {
            return;
        }

        var delay = respawnDelayTicks < 0 ? 0 : respawnDelayTicks;
        _respawnAt[nodeId] = currentTick + delay;
    }

    public void Deplete(GridCoordinate coordinate, long currentTick, long respawnDelayTicks)
    {
        if (TryGetByCoordinate(coordinate, out var anchor))
        {
            Deplete(anchor.NodeId, currentTick, respawnDelayTicks);
        }
    }

    public IReadOnlyList<ResourceSpawnAnchor> Tick(long currentTick)
    {
        List<string>? respawned = null;
        foreach (var pair in _respawnAt)
        {
            if (currentTick >= pair.Value)
            {
                (respawned ??= new List<string>()).Add(pair.Key);
            }
        }

        if (respawned is null)
        {
            return System.Array.Empty<ResourceSpawnAnchor>();
        }

        foreach (var nodeId in respawned)
        {
            _respawnAt.Remove(nodeId);
            _active.Add(nodeId);
        }

        return respawned.Select(nodeId => _anchorsById[nodeId]).ToArray();
    }
}
