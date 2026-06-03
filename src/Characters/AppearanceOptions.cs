namespace DawnOfBlade.Characters;

/// <summary>Catalog of allowed customization choices, shared by the player creator and NPC randomizer.</summary>
public static class AppearanceOptions
{
    public static readonly string[] BodyTypes = { "slim", "broad" };
    public static readonly string[] Presentations = { "masculine", "feminine" };

    public const int HairStyleCount = 8;
    public const int ShapeStyleCount = 4;

    public static readonly string[] SkinTones =
    {
        "#f0c8a0", "#e0b48c", "#c68642", "#8d5524", "#5a3a22", "#d7a06f", "#6e4934",
    };

    public static readonly string[] HairColors =
    {
        "#1a1a1a", "#3a2a1a", "#7a4a1a", "#b08030", "#9a9a9a", "#b03030", "#e7e0c8", "#8b2fd6",
    };

    public static readonly string[] ShirtColors =
    {
        "#6a5acd", "#3a7a3a", "#a03030", "#3060a0", "#9a7a30", "#444a55", "#7f254f", "#2f7c74", "#4b8d38",
    };

    public static readonly string[] LegColors =
    {
        "#3b3b46", "#5a4632", "#2f4f4f", "#4a2f3a", "#32405c", "#31452e",
    };

    public static readonly string[] FootColors =
    {
        "#4a3324", "#2d2522", "#73553a", "#59412c", "#242831",
    };
}
