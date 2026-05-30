namespace DawnOfBlade.Characters;

/// <summary>
/// Visual customization for a character. Colors are stored as hex strings so they serialize
/// cleanly into the save file and parse into Godot colors at display time.
/// </summary>
public sealed class Appearance
{
    public string BodyType { get; set; } = "slim";
    public int HairStyle { get; set; } = 0;
    public string SkinTone { get; set; } = "#e0b48c";
    public string HairColor { get; set; } = "#3a2a1a";
    public string ShirtColor { get; set; } = "#6a5acd";
    public string LegColor { get; set; } = "#3b3b46";

    public Appearance Clone() => (Appearance)MemberwiseClone();
}
