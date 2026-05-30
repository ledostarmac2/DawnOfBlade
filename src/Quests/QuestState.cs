using System.Collections.Generic;
using System.Linq;

namespace DawnOfBlade.Quests;

/// <summary>
/// Tracks live progress for a single quest: per-objective counts and completion.
/// Pure C# so it can be exercised without Godot.
/// </summary>
public sealed class QuestState
{
    private readonly Dictionary<string, int> _progress = new();

    public QuestState(QuestDefinition definition, IReadOnlyDictionary<string, int>? savedProgress = null)
    {
        Definition = definition;

        foreach (var objective in definition.Objectives)
        {
            var saved = 0;
            savedProgress?.TryGetValue(objective.Id, out saved);
            _progress[objective.Id] = System.Math.Clamp(saved, 0, objective.RequiredCount);
        }

        RecomputeCompletion();
    }

    public QuestDefinition Definition { get; }

    public bool IsComplete { get; private set; }

    /// <summary>Set once rewards have been handed out so they are not granted twice.</summary>
    public bool RewardsGranted { get; set; }

    public IReadOnlyDictionary<string, int> Progress => _progress;

    public int GetProgress(string objectiveId) =>
        _progress.TryGetValue(objectiveId, out var value) ? value : 0;

    public bool IsObjectiveComplete(string objectiveId)
    {
        var objective = Definition.Objectives.FirstOrDefault(o => o.Id == objectiveId);
        return objective is not null && GetProgress(objectiveId) >= objective.RequiredCount;
    }

    /// <summary>Advances an objective by <paramref name="amount"/>, clamped to its required count.</summary>
    public void Advance(string objectiveId, int amount = 1)
    {
        var objective = Definition.Objectives.FirstOrDefault(o => o.Id == objectiveId);
        if (objective is null || amount <= 0)
        {
            return;
        }

        _progress[objectiveId] = System.Math.Clamp(GetProgress(objectiveId) + amount, 0, objective.RequiredCount);
        RecomputeCompletion();
    }

    private void RecomputeCompletion()
    {
        IsComplete = Definition.Objectives.All(o => GetProgress(o.Id) >= o.RequiredCount);
    }
}
