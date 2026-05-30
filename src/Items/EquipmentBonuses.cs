namespace DawnOfBlade.Items;

/// <summary>Aggregated combat bonuses contributed by worn equipment.</summary>
public readonly record struct EquipmentBonuses(int Attack, int Strength, int Defense)
{
    public static EquipmentBonuses Zero => new(0, 0, 0);

    public EquipmentBonuses Add(EquipmentBonuses other) =>
        new(Attack + other.Attack, Strength + other.Strength, Defense + other.Defense);

    public int Score => Attack + Strength + Defense;
}
