namespace DawnOfBlade.Inventory;

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Description,
    bool Stackable,
    int Value
);

