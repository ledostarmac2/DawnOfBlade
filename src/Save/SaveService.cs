using System.Linq;
using Godot;

namespace DawnOfBlade.Save;

/// <summary>
/// Reads and writes an account-scoped local save file using Godot's virtual filesystem.
/// Serialization itself lives in <see cref="SaveSerializer"/>.
/// </summary>
public sealed class SaveService
{
    private const string LegacySavePath = "user://savegame.json";
    private readonly string _savePath;

    public SaveService(string? username = null)
    {
        _savePath = BuildSavePath(username);
    }

    public string SavePath => _savePath;
    public bool SaveExists => FileAccess.FileExists(_savePath) || FileAccess.FileExists(LegacySavePath);

    public bool Save(SaveGame save)
    {
        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushWarning($"Could not write save file: {FileAccess.GetOpenError()}");
            return false;
        }

        file.StoreString(SaveSerializer.ToJson(save));
        return true;
    }

    public SaveGame Load()
    {
        if (!SaveExists)
        {
            return new SaveGame();
        }

        var path = FileAccess.FileExists(_savePath) ? _savePath : LegacySavePath;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"Could not read save file: {FileAccess.GetOpenError()}");
            return new SaveGame();
        }

        var save = SaveSerializer.FromJson(file.GetAsText());
        if (path == LegacySavePath && _savePath != LegacySavePath && Save(save))
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(LegacySavePath));
        }

        return save;
    }

    public static string BuildSavePath(string? username)
    {
        var account = string.IsNullOrWhiteSpace(username) ? "guest" : username.Trim().ToLowerInvariant();
        var safe = new string(account.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());
        return $"user://savegame_{safe}.json";
    }
}
