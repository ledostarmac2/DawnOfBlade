namespace DawnOfBlade.Engine.Ai;

/// <summary>
/// Behavioral class of a wandering actor. Determines whether — and under what conditions — the
/// actor will initiate combat with a perceived target. Aggression is a function of this archetype
/// <em>and</em> the relative combat levels of the actor and its target (see
/// <see cref="AggressionPolicy"/>).
/// </summary>
public enum MonsterArchetype
{
    /// <summary>Never initiates. Wanders its area only. Used for town NPCs and harmless critters.</summary>
    Passive,

    /// <summary>Never initiates, but is expected to retaliate once struck. Wanders like a passive.</summary>
    Defensive,

    /// <summary>
    /// Initiates against any target inside its aggro radius, but loses interest in a target whose
    /// combat level has outgrown it (the classic "stronger players are ignored" rule).
    /// </summary>
    Aggressive,

    /// <summary>
    /// Initiates against any target inside its aggro radius regardless of how strong the target is.
    /// Used for bosses and dedicated hunters that never stop being a threat.
    /// </summary>
    Predator,
}
