namespace DawnOfBlade.Engine.Tick;

/// <summary>The category of incoming damage an overhead mitigation reduces.</summary>
public enum MitigationKind
{
    Melee,
    Ranged,
    Magic,
    Environmental,
}

/// <summary>
/// An overhead/environmental mitigation toggle. Combat reads <see cref="IsActive"/> at the exact
/// moment damage is calculated (Phase 3), so a toggle applied earlier in the same tick (Phase 0)
/// is honored precisely.
/// </summary>
public sealed class OverheadMitigation
{
    public OverheadMitigation(MitigationKind kind, bool active = false)
    {
        Kind = kind;
        IsActive = active;
    }

    public MitigationKind Kind { get; }

    public bool IsActive { get; set; }

    /// <summary>True if this mitigation applies to the given incoming damage kind right now.</summary>
    public bool Blocks(MitigationKind incoming) => IsActive && Kind == incoming;
}
