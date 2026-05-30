using System.Collections.Generic;

namespace DawnOfBlade.Quests;

/// <summary>
/// Holds the player's active quests and routes objective progress to them. Pure C#.
/// </summary>
public sealed class QuestLog
{
    private readonly Dictionary<string, QuestState> _quests = new();

    public IReadOnlyCollection<QuestState> Quests => _quests.Values;

    public QuestState Start(QuestDefinition definition, IReadOnlyDictionary<string, int>? savedProgress = null)
    {
        var state = new QuestState(definition, savedProgress);
        _quests[definition.Id] = state;
        return state;
    }

    public QuestState? Get(string questId) =>
        _quests.TryGetValue(questId, out var state) ? state : null;

    public bool IsActive(string questId) => _quests.ContainsKey(questId);

    /// <summary>
    /// Advances the matching objective in every active quest and returns the quests that became
    /// complete on this call (so the caller can grant their rewards exactly once).
    /// </summary>
    public IReadOnlyList<QuestState> Advance(string objectiveId, int amount = 1)
    {
        var newlyComplete = new List<QuestState>();

        foreach (var state in _quests.Values)
        {
            var wasComplete = state.IsComplete;
            state.Advance(objectiveId, amount);
            if (!wasComplete && state.IsComplete)
            {
                newlyComplete.Add(state);
            }
        }

        return newlyComplete;
    }
}
