using System.Collections.Generic;

namespace DawnOfBlade.Dialogue;

public sealed record DialogueChoice(string Text, string? NextNodeId);

public sealed record DialogueNode(
    string Id,
    string SpeakerId,
    string Text,
    IReadOnlyList<DialogueChoice> Choices
);

