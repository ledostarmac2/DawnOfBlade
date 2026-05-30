using System.Text.Json;

namespace DawnOfBlade.Save;

/// <summary>
/// Converts a <see cref="SaveGame"/> to and from JSON. Engine-independent so save round-trips
/// can be unit tested without Godot.
/// </summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string ToJson(SaveGame save) => JsonSerializer.Serialize(save, Options);

    public static SaveGame FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SaveGame();
        }

        return JsonSerializer.Deserialize<SaveGame>(json, Options) ?? new SaveGame();
    }
}
