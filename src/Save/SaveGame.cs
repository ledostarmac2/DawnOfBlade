using System.Collections.Generic;
using DawnOfBlade.Characters;

namespace DawnOfBlade.Save;

public sealed class SaveGame
{
    public string Version { get; set; } = "0.1.0";
    public string PlayerName { get; set; } = "Player";
    public string Server { get; set; } = "";
    public float[] PlayerPosition { get; set; } = new[] { 0.0f, 0.0f, 0.0f };
    public Dictionary<string, int> Inventory { get; set; } = new();
    public Dictionary<string, int> SkillExperience { get; set; } = new();
    public HashSet<string> CompletedQuestIds { get; set; } = new();
    public HashSet<string> UnlockedVocabularyIds { get; set; } = new();

    /// <summary>Per-quest, per-objective progress counts for quests still in progress.</summary>
    public Dictionary<string, Dictionary<string, int>> QuestProgress { get; set; } = new();

    /// <summary>Worn equipment keyed by <see cref="Items.EquipmentSlot"/> name -> item id.</summary>
    public Dictionary<string, string> Equipment { get; set; } = new();

    public Appearance Appearance { get; set; } = new();
}
