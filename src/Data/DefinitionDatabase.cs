using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DawnOfBlade.Dialogue;
using DawnOfBlade.Inventory;
using DawnOfBlade.Items;
using DawnOfBlade.Learning;
using DawnOfBlade.Quests;
using DawnOfBlade.Shops;
using DawnOfBlade.Skills;
using Godot;

namespace DawnOfBlade.Data;

/// <summary>
/// Loads JSON content definitions from <c>res://data/</c> into strongly typed records and
/// exposes id-keyed lookups. Parsing is engine-independent (<see cref="ParseList{T}"/>) so it
/// can be unit tested off-engine; only <see cref="Load"/> touches Godot's virtual filesystem.
/// </summary>
public sealed class DefinitionDatabase
{
    public const string ItemsPath = "res://data/items/items.example.json";
    public const string SkillsPath = "res://data/skills/skills.example.json";
    public const string NpcsPath = "res://data/npcs/npcs.example.json";
    public const string DialoguePath = "res://data/dialogue/dialogue.example.json";
    public const string QuestsPath = "res://data/quests/quests.example.json";
    public const string VocabularyPath = "res://data/vocabulary/vocabulary.example.json";
    public const string EquipmentPath = "res://data/equipment/equipment.example.json";
    public const string ShopsPath = "res://data/shops/shops.example.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public IReadOnlyDictionary<string, ItemDefinition> ItemById { get; private set; } =
        new Dictionary<string, ItemDefinition>();
    public IReadOnlyDictionary<string, SkillDefinition> SkillById { get; private set; } =
        new Dictionary<string, SkillDefinition>();
    public IReadOnlyDictionary<string, NpcDefinition> NpcById { get; private set; } =
        new Dictionary<string, NpcDefinition>();
    public IReadOnlyDictionary<string, DialogueNode> DialogueById { get; private set; } =
        new Dictionary<string, DialogueNode>();
    public IReadOnlyDictionary<string, QuestDefinition> QuestById { get; private set; } =
        new Dictionary<string, QuestDefinition>();
    public IReadOnlyList<VocabularyEntry> Vocabulary { get; private set; } = new List<VocabularyEntry>();
    public IReadOnlyDictionary<string, EquipmentDefinition> EquipmentByItemId { get; private set; } =
        new Dictionary<string, EquipmentDefinition>();
    public IReadOnlyDictionary<string, ShopDefinition> ShopById { get; private set; } =
        new Dictionary<string, ShopDefinition>();

    /// <summary>Loads every definition file from the Godot resource filesystem.</summary>
    public void Load()
    {
        ItemById = ParseList<ItemDefinition>(ReadText(ItemsPath)).ToDictionary(i => i.Id);
        SkillById = ParseList<SkillDefinition>(ReadText(SkillsPath)).ToDictionary(s => s.Id);
        NpcById = ParseList<NpcDefinition>(ReadText(NpcsPath)).ToDictionary(n => n.Id);
        DialogueById = ParseList<DialogueNode>(ReadText(DialoguePath)).ToDictionary(d => d.Id);
        QuestById = ParseList<QuestDefinition>(ReadText(QuestsPath)).ToDictionary(q => q.Id);
        Vocabulary = ParseList<VocabularyEntry>(ReadText(VocabularyPath));
        EquipmentByItemId = ParseList<EquipmentDefinition>(ReadText(EquipmentPath)).ToDictionary(e => e.ItemId);
        ShopById = ParseList<ShopDefinition>(ReadText(ShopsPath)).ToDictionary(s => s.Id);
    }

    public EquipmentDefinition? FindEquipment(string itemId) =>
        EquipmentByItemId.TryGetValue(itemId, out var definition) ? definition : null;

    /// <summary>Parses a JSON array into a list of records. Engine-independent and unit testable.</summary>
    public static IReadOnlyList<T> ParseList<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<T>();
        }

        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }

    private static string ReadText(string resourcePath)
    {
        using var file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"Definition file not found: {resourcePath} ({FileAccess.GetOpenError()})");
            return string.Empty;
        }

        return file.GetAsText();
    }
}
