using System;
using System.IO;
using System.Linq;
using DawnOfBlade.Data;
using DawnOfBlade.Dialogue;
using DawnOfBlade.Inventory;
using DawnOfBlade.Quests;
using DawnOfBlade.Skills;
using Xunit;

namespace DawnOfBlade.Tests;

public class DefinitionParseTests
{
    [Fact]
    public void ParseList_ReadsItemFields()
    {
        const string json = """
        [{ "id": "sunleaf", "displayName": "Sunleaf", "description": "An herb.", "stackable": true, "value": 3 }]
        """;

        var items = DefinitionDatabase.ParseList<ItemDefinition>(json);

        var item = Assert.Single(items);
        Assert.Equal("sunleaf", item.Id);
        Assert.Equal("Sunleaf", item.DisplayName);
        Assert.True(item.Stackable);
        Assert.Equal(3, item.Value);
    }

    [Fact]
    public void ParseList_HandlesEmptyAndNull()
    {
        Assert.Empty(DefinitionDatabase.ParseList<ItemDefinition>(""));
        Assert.Empty(DefinitionDatabase.ParseList<ItemDefinition>("[]"));
    }

    // --- Real content validation (data schema validation for JSON definitions) ---

    [Fact]
    public void Items_AreValidAndUnique()
    {
        var items = DefinitionDatabase.ParseList<ItemDefinition>(DataText("items/items.example.json"));

        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.Id)));
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.DisplayName)));
        AssertUniqueIds(items.Select(i => i.Id));
    }

    [Fact]
    public void Skills_AreValidAndUnique()
    {
        var skills = DefinitionDatabase.ParseList<SkillDefinition>(DataText("skills/skills.example.json"));

        Assert.NotEmpty(skills);
        Assert.All(skills, s => Assert.True(s.MaxLevel > 0));
        AssertUniqueIds(skills.Select(s => s.Id));
    }

    [Fact]
    public void Quests_HaveObjectivesAndParseableRewards()
    {
        var quests = DefinitionDatabase.ParseList<QuestDefinition>(DataText("quests/quests.example.json"));

        Assert.NotEmpty(quests);
        Assert.All(quests, q => Assert.NotEmpty(q.Objectives));
        Assert.All(quests, q => Assert.All(q.Objectives, o => Assert.True(o.RequiredCount > 0)));
        Assert.All(quests, q => Assert.All(q.Rewards, token =>
            Assert.True(QuestReward.TryParse(token, out _), $"Unparseable reward token: {token}")));
    }

    [Fact]
    public void Dialogue_NodesParse()
    {
        var nodes = DefinitionDatabase.ParseList<DialogueNode>(DataText("dialogue/dialogue.example.json"));

        Assert.NotEmpty(nodes);
        AssertUniqueIds(nodes.Select(n => n.Id));
    }

    [Fact]
    public void Npcs_HaveDistinctDialogueRoots()
    {
        var npcs = DefinitionDatabase.ParseList<NpcDefinition>(DataText("npcs/npcs.example.json"));
        var dialogue = DefinitionDatabase.ParseList<DialogueNode>(DataText("dialogue/dialogue.example.json"))
            .ToDictionary(node => node.Id);

        Assert.Contains(npcs, npc => npc.Id == "mira_tutor");
        Assert.Contains(npcs, npc => npc.Id == "bran_quartermaster");
        Assert.Contains(npcs, npc => npc.Id == "lysa_ranger");
        Assert.Contains(npcs, npc => npc.Id == "orin_woodcutter");
        Assert.All(npcs, npc => Assert.True(dialogue.ContainsKey(npc.DialogueRootId), $"{npc.Id} dialogue root missing"));
        Assert.True(npcs.Select(npc => npc.DialogueRootId).Distinct().Count() >= 4);
    }

    private static void AssertUniqueIds(System.Collections.Generic.IEnumerable<string> ids)
    {
        var list = ids.ToList();
        Assert.Equal(list.Count, list.Distinct().Count());
    }

    private static string DataText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "data", relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DawnOfBlade.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate repo root from test output.");
    }
}
