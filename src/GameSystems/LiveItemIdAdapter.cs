using System;
using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.GameSystems.Content;

namespace DawnOfBlade.GameSystems;

public sealed record ItemIdBinding(int GameItemId, string LiveItemId);

/// <summary>
/// Strict boundary between engine-independent numeric item ids and the live Godot catalog ids.
/// Unknown ids are integration errors; callers that intentionally probe can use the Try methods.
/// </summary>
public sealed class LiveItemIdAdapter
{
    private readonly IReadOnlyDictionary<int, string> _liveIdsByGameItemId;
    private readonly IReadOnlyDictionary<string, int> _gameItemIdsByLiveId;

    public LiveItemIdAdapter(IEnumerable<ItemIdBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var materialized = bindings.ToArray();
        if (materialized.Any(binding => string.IsNullOrWhiteSpace(binding.LiveItemId)))
        {
            throw new ArgumentException("Live item ids must not be empty.", nameof(bindings));
        }

        var duplicateGameItemId = materialized.GroupBy(binding => binding.GameItemId).FirstOrDefault(group => group.Count() > 1);
        if (duplicateGameItemId is not null)
        {
            throw new ArgumentException($"Duplicate game item id: {duplicateGameItemId.Key}.", nameof(bindings));
        }

        var duplicateLiveItemId = materialized
            .GroupBy(binding => binding.LiveItemId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateLiveItemId is not null)
        {
            throw new ArgumentException($"Duplicate live item id: {duplicateLiveItemId.Key}.", nameof(bindings));
        }

        Bindings = materialized;
        _liveIdsByGameItemId = materialized.ToDictionary(binding => binding.GameItemId, binding => binding.LiveItemId);
        _gameItemIdsByLiveId = materialized.ToDictionary(binding => binding.LiveItemId, binding => binding.GameItemId, StringComparer.Ordinal);
    }

    public static LiveItemIdAdapter StartingRegion { get; } = new(new[]
    {
        new ItemIdBinding(RegionItemIds.Coins, "coins"),
        new ItemIdBinding(RegionItemIds.CopperOre, "copper_ore"),
        new ItemIdBinding(RegionItemIds.TinOre, "tin_ore"),
        new ItemIdBinding(RegionItemIds.SoftwoodLogs, "logs"),
        new ItemIdBinding(RegionItemIds.RawWool, "raw_wool"),
        new ItemIdBinding(RegionItemIds.BronzeBar, "bronze_bar"),
        new ItemIdBinding(RegionItemIds.BronzeDagger, "bronze_dagger"),
        new ItemIdBinding(RegionItemIds.Feathers, "feathers"),
        new ItemIdBinding(RegionItemIds.RawPoultry, "raw_poultry"),
        new ItemIdBinding(RegionItemIds.BrittleBones, "brittle_bones"),
    });

    public IReadOnlyCollection<ItemIdBinding> Bindings { get; }

    public bool TryToLiveId(int gameItemId, out string liveItemId) =>
        _liveIdsByGameItemId.TryGetValue(gameItemId, out liveItemId!);

    public string ToLiveId(int gameItemId) =>
        TryToLiveId(gameItemId, out var liveItemId)
            ? liveItemId
            : throw new KeyNotFoundException($"No live item id is registered for game item id {gameItemId}.");

    public bool TryToGameItemId(string liveItemId, out int gameItemId) =>
        _gameItemIdsByLiveId.TryGetValue(liveItemId, out gameItemId);

    public int ToGameItemId(string liveItemId) =>
        TryToGameItemId(liveItemId, out var gameItemId)
            ? gameItemId
            : throw new KeyNotFoundException($"No game item id is registered for live item id '{liveItemId}'.");
}
