using System.Collections.Generic;

namespace DawnOfBlade.Quests;

public sealed record QuestObjective(
    string Id,
    string Description,
    int RequiredCount
);

public sealed record QuestDefinition(
    string Id,
    string Title,
    string Description,
    IReadOnlyList<QuestObjective> Objectives,
    IReadOnlyList<string> Rewards
);

