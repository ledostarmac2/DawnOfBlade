namespace DawnOfBlade.UI.Presentation;

public enum HitMarkerType
{
    Miss,
    Normal,
    Arcane,
    Critical,
    Block,
}

/// <summary>Rendering metadata for one projected overhead combat marker.</summary>
public sealed record HitMarkerPresentation(
    string TargetEntityId,
    int Damage,
    HitMarkerType Type,
    float HorizontalOffset,
    double LifetimeSeconds)
{
    public const double StandardLifetimeSeconds = 0.8;
    public const double CriticalLifetimeSeconds = 1.0;

    public static HitMarkerPresentation Create(
        string targetEntityId,
        int damage,
        HitMarkerType type,
        float horizontalOffset)
    {
        if (string.IsNullOrWhiteSpace(targetEntityId))
        {
            throw new System.ArgumentException("A target entity id is required.", nameof(targetEntityId));
        }

        return new HitMarkerPresentation(
            targetEntityId,
            System.Math.Max(0, damage),
            type,
            System.Math.Clamp(horizontalOffset, -20, 20),
            type == HitMarkerType.Critical ? CriticalLifetimeSeconds : StandardLifetimeSeconds);
    }
}
