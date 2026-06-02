namespace DawnOfBlade.World.Grid;

public enum PlayerVersusPlayerRule
{
    Disabled,
    Localized,
    Unrestricted,
}

public enum DeathDropRule
{
    KeepInventory,
    DropAllCarriedAndEquipped,
}

/// <summary>Region-level gameplay rules independent of scene presentation.</summary>
public sealed record WorldZone(
    string Id,
    string DisplayName,
    PlayerVersusPlayerRule PlayerVersusPlayer,
    DeathDropRule DeathDrops,
    bool IsSafeZone)
{
    public static readonly WorldZone VerdantValley = new(
        "verdant_valley", "Verdant Valley", PlayerVersusPlayerRule.Disabled, DeathDropRule.KeepInventory, true);

    public static readonly WorldZone SunscorchedDunes = new(
        "sunscorched_dunes", "Sunscorched Dunes", PlayerVersusPlayerRule.Localized, DeathDropRule.KeepInventory, false);

    public static readonly WorldZone WhisperingMire = new(
        "whispering_mire", "Whispering Mire", PlayerVersusPlayerRule.Unrestricted, DeathDropRule.DropAllCarriedAndEquipped, false);
}
