namespace DawnOfBlade.Items;

/// <summary>
/// Content definition that makes an item wearable. Keyed by item id; <c>Slot</c> is parsed to
/// <see cref="EquipmentSlot"/> at runtime. See <c>data/equipment/equipment.example.json</c>.
/// </summary>
public sealed record EquipmentDefinition(
    string ItemId,
    string Slot,
    int AttackBonus,
    int StrengthBonus,
    int DefenseBonus,
    int RequiredAttack = 1,
    int RequiredDefense = 1
);
