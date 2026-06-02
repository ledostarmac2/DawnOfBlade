using System.Collections.Generic;
using DawnOfBlade.Save;
using Xunit;

namespace DawnOfBlade.Tests;

public class SaveSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesState()
    {
        var original = new SaveGame
        {
            PlayerName = "Brennan",
            Server = "Northwind Realm",
            PlayerPosition = new[] { 1.5f, 0.9f, -2.25f },
            Inventory = new Dictionary<string, int> { ["sunleaf"] = 3, ["practice_chisel"] = 1 },
            SkillExperience = new Dictionary<string, int> { ["foraging"] = 75, ["language"] = 25 },
            UnlockedVocabularyIds = new HashSet<string> { "hello", "path" },
            CompletedQuestIds = new HashSet<string> { "first_words" },
            QuestProgress = new Dictionary<string, Dictionary<string, int>>
            {
                ["first_words"] = new() { ["collect_sunleaf"] = 3, ["answer_prompt"] = 1 },
            },
        };

        var restored = SaveSerializer.FromJson(SaveSerializer.ToJson(original));

        Assert.Equal(original.PlayerName, restored.PlayerName);
        Assert.Equal(original.Server, restored.Server);
        Assert.Equal(original.PlayerPosition, restored.PlayerPosition);
        Assert.Equal(original.Inventory, restored.Inventory);
        Assert.Equal(original.SkillExperience, restored.SkillExperience);
        Assert.Equal(original.UnlockedVocabularyIds, restored.UnlockedVocabularyIds);
        Assert.Equal(original.CompletedQuestIds, restored.CompletedQuestIds);
        Assert.Equal(3, restored.QuestProgress["first_words"]["collect_sunleaf"]);
    }

    [Fact]
    public void FromJson_HandlesEmptyInput()
    {
        var save = SaveSerializer.FromJson(null);

        Assert.NotNull(save);
        Assert.Empty(save.Inventory);
    }

    [Theory]
    [InlineData("Ledostar", "user://savegame_ledostar.json")]
    [InlineData("A Name!", "user://savegame_a_name_.json")]
    [InlineData(null, "user://savegame_guest.json")]
    public void BuildSavePath_IsAccountScopedAndFilesystemSafe(string? username, string expected)
    {
        Assert.Equal(expected, SaveService.BuildSavePath(username));
    }
}
