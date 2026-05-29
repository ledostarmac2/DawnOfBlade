using System.Collections.Generic;

namespace DawnOfBlade.Learning;

public sealed record LearningPrompt(
    string PromptText,
    string CorrectAnswer,
    IReadOnlyList<string> Choices,
    string SkillTag,
    int Difficulty
);

