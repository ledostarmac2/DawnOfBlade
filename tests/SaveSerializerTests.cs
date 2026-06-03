using System.Collections.Generic;
using DawnOfBlade.Auth;
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
            SkillExperience = new Dictionary<string, int> { ["foraging"] = 75, ["smithing"] = 25 },
            CompletedQuestIds = new HashSet<string> { "first_words" },
            QuestProgress = new Dictionary<string, Dictionary<string, int>>
            {
                ["first_words"] = new() { ["collect_sunleaf"] = 3 },
            },
        };

        var restored = SaveSerializer.FromJson(SaveSerializer.ToJson(original));

        Assert.Equal(original.PlayerName, restored.PlayerName);
        Assert.Equal(original.Server, restored.Server);
        Assert.Equal(original.PlayerPosition, restored.PlayerPosition);
        Assert.Equal(original.Inventory, restored.Inventory);
        Assert.Equal(original.SkillExperience, restored.SkillExperience);
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
    [InlineData("Ledostar", "user://savegame_ledostar_f3d5ebb2403eeb7e.json")]
    [InlineData(null, "user://savegame_guest_84983c60f7daadc1.json")]
    public void BuildSavePath_IsAccountScopedAndFilesystemSafe(string? username, string expected)
    {
        Assert.Equal(expected, SaveService.BuildSavePath(username));
    }

    [Fact]
    public void AccountIdentity_NormalizesConsistentlyAndAvoidsSanitizedPathCollisions()
    {
        Assert.Equal("ledostar", AccountIdentity.NormalizeUsername("  Ledostar "));
        Assert.NotEqual(SaveService.BuildSavePath("a!b"), SaveService.BuildSavePath("a?b"));
        Assert.Equal("user://savegame_a_b.json", SaveService.BuildLegacyAccountSavePath("A!B"));
    }

    [Fact]
    public void SaveService_LoadCandidatesPreferNewThenBackupThenLegacyPaths()
    {
        var service = new SaveService("Ledostar");

        Assert.Equal(service.SavePath, service.LoadCandidates()[0]);
        Assert.Equal($"{service.SavePath}.bak", service.LoadCandidates()[1]);
        Assert.Equal("user://savegame_ledostar.json", service.LoadCandidates()[2]);
        Assert.Equal("user://savegame.json", service.LoadCandidates()[3]);
    }
}
