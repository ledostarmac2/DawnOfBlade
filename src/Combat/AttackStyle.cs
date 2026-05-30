namespace DawnOfBlade.Combat;

/// <summary>
/// Chosen melee style. Each trades off accuracy, damage, or defense and trains a
/// different combat skill on a successful hit.
/// </summary>
public enum AttackStyle
{
    /// <summary>Higher accuracy, trains Attack.</summary>
    Accurate,

    /// <summary>Higher damage, trains Strength.</summary>
    Aggressive,

    /// <summary>Higher defense, trains Defense.</summary>
    Defensive,
}
