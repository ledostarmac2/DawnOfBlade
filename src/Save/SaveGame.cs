using System.Collections.Generic;

namespace DawnOfBlade.Save;

public sealed class SaveGame
{
    public string Version { get; set; } = "0.1.0";
    public string PlayerName { get; set; } = "Player";
    public float[] PlayerPosition { get; set; } = new[] { 0.0f, 0.0f, 0.0f };
    public Dictionary<string, int> Inventory { get; set; } = new();
    public Dictionary<string, int> SkillExperience { get; set; } = new();
    public HashSet<string> CompletedQuestIds { get; set; } = new();
    public HashSet<string> UnlockedVocabularyIds { get; set; } = new();
}
