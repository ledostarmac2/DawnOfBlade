namespace DawnOfBlade.Data;

public sealed record NpcDefinition(
    string Id,
    string DisplayName,
    string Role,
    string DialogueRootId
);
