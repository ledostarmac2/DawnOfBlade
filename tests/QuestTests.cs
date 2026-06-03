using System.Collections.Generic;
using DawnOfBlade.Quests;
using Xunit;

namespace DawnOfBlade.Tests;

public class QuestTests
{
    private static QuestDefinition SampleQuest() => new(
        "first_words",
        "A Traveler's Errand",
        "Help Mira at the crossroads camp.",
        new List<QuestObjective>
        {
            new("collect_sunleaf", "Collect three Sunleaf.", 3),
            new("return_to_camp", "Return to the camp.", 1),
        },
        new List<string> { "xp:foraging:25", "item:practice_chisel:1" });

    [Theory]
    [InlineData("xp:foraging:25", true, "xp", "foraging", 25)]
    [InlineData("item:practice_chisel:1", true, "item", "practice_chisel", 1)]
    [InlineData("xp:foraging", false, "", "", 0)]
    [InlineData("xp:foraging:notanumber", false, "", "", 0)]
    public void QuestReward_Parses(string token, bool expectedOk, string kind, string target, int amount)
    {
        var ok = QuestReward.TryParse(token, out var reward);

        Assert.Equal(expectedOk, ok);
        if (expectedOk)
        {
            Assert.Equal(kind, reward.Kind);
            Assert.Equal(target, reward.Target);
            Assert.Equal(amount, reward.Amount);
        }
    }

    [Fact]
    public void Advance_ClampsToRequiredCount()
    {
        var state = new QuestState(SampleQuest());
        state.Advance("collect_sunleaf", 10);

        Assert.Equal(3, state.GetProgress("collect_sunleaf"));
        Assert.True(state.IsObjectiveComplete("collect_sunleaf"));
    }

    [Fact]
    public void IsComplete_RequiresAllObjectives()
    {
        var state = new QuestState(SampleQuest());
        state.Advance("collect_sunleaf", 3);
        Assert.False(state.IsComplete);

        state.Advance("return_to_camp", 1);
        Assert.True(state.IsComplete);
    }

    [Fact]
    public void RestoresFromSavedProgress()
    {
        var state = new QuestState(SampleQuest(), new Dictionary<string, int> { ["collect_sunleaf"] = 3 });

        Assert.Equal(3, state.GetProgress("collect_sunleaf"));
        Assert.False(state.IsComplete);
    }

    [Fact]
    public void QuestLog_ReportsNewlyCompletedOnce()
    {
        var log = new QuestLog();
        log.Start(SampleQuest());

        Assert.Empty(log.Advance("collect_sunleaf", 3));

        var completed = log.Advance("return_to_camp", 1);
        Assert.Single(completed);
        Assert.Equal("first_words", completed[0].Definition.Id);

        // Advancing again must not report it as newly complete a second time.
        Assert.Empty(log.Advance("return_to_camp", 1));
    }
}
