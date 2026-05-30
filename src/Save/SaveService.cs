using Godot;

namespace DawnOfBlade.Save;

/// <summary>
/// Reads and writes the local save file at <c>user://savegame.json</c> using Godot's
/// virtual filesystem. Serialization itself lives in <see cref="SaveSerializer"/>.
/// </summary>
public sealed class SaveService
{
    public const string SavePath = "user://savegame.json";

    public bool SaveExists => FileAccess.FileExists(SavePath);

    public bool Save(SaveGame save)
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
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

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"Could not read save file: {FileAccess.GetOpenError()}");
            return new SaveGame();
        }

        return SaveSerializer.FromJson(file.GetAsText());
    }
}
