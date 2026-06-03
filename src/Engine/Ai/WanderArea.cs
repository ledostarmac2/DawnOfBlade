using DawnOfBlade.Engine.Spatial;

namespace DawnOfBlade.Engine.Ai;

/// <summary>
/// The fixed territory an actor belongs to, centered on its spawn <see cref="Anchor"/>. The actor
/// only picks wander destinations inside <see cref="WanderRadius"/>, and abandons a chase once it
/// strays beyond <see cref="LeashRadius"/> from the anchor — so monsters and NPCs stay home and
/// can never be kited across the map.
/// </summary>
/// <param name="Anchor">Spawn tile; the center of the territory.</param>
/// <param name="WanderRadius">Chebyshev radius the actor may roam while idle/wandering.</param>
/// <param name="LeashRadius">Chebyshev radius beyond which a chase breaks and the actor returns.</param>
public readonly record struct WanderArea(TrueTile Anchor, int WanderRadius, int LeashRadius)
{
    /// <summary>True if <paramref name="tile"/> is within the wander radius of the anchor.</summary>
    public bool ContainsWander(TrueTile tile) => Anchor.ChebyshevDistance(tile) <= WanderRadius;

    /// <summary>True if <paramref name="tile"/> is still within the chase leash of the anchor.</summary>
    public bool WithinLeash(TrueTile tile) => Anchor.ChebyshevDistance(tile) <= LeashRadius;
}
