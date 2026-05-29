namespace DawnOfBlade.Skills;

public sealed record SkillDefinition(
    string Id,
    string DisplayName,
    string Description,
    int MaxLevel = 99
);

