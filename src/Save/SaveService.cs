using System;
using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Auth;
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
    private readonly string _legacyAccountSavePath;

    public SaveService(string? username = null)
    {
        _savePath = BuildSavePath(username);
        _legacyAccountSavePath = BuildLegacyAccountSavePath(username);
    }

    public string SavePath => _savePath;
    public bool SaveExists => LoadCandidates().Any(FileAccess.FileExists);

    public bool Save(SaveGame save)
    {
        var tempPath = $"{_savePath}.tmp";
        var backupPath = $"{_savePath}.bak";
        var previousBackupPath = $"{backupPath}.previous";
        using var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushWarning($"Could not write temporary save file: {FileAccess.GetOpenError()}");
            return false;
        }

        file.StoreString(SaveSerializer.ToJson(save));
        file.Close();

        var target = ProjectSettings.GlobalizePath(_savePath);
        var temp = ProjectSettings.GlobalizePath(tempPath);
        var backup = ProjectSettings.GlobalizePath(backupPath);
        var previousBackup = ProjectSettings.GlobalizePath(previousBackupPath);
        DirAccess.RemoveAbsolute(previousBackup);
        if (FileAccess.FileExists(backupPath) && DirAccess.RenameAbsolute(backup, previousBackup) != Error.Ok)
        {
            GD.PushWarning("Could not preserve the previous save backup.");
            DirAccess.RemoveAbsolute(temp);
            return false;
        }

        if (FileAccess.FileExists(_savePath) && DirAccess.RenameAbsolute(target, backup) != Error.Ok)
        {
            GD.PushWarning("Could not rotate the previous save file.");
            RestorePreviousBackup(previousBackupPath, previousBackup, backup);
            DirAccess.RemoveAbsolute(temp);
            return false;
        }

        if (DirAccess.RenameAbsolute(temp, target) == Error.Ok)
        {
            if (FileAccess.FileExists(previousBackupPath))
            {
                DirAccess.RemoveAbsolute(backup);
                RestorePreviousBackup(previousBackupPath, previousBackup, backup);
            }

            return true;
        }

        GD.PushWarning("Could not promote the temporary save file.");
        if (FileAccess.FileExists(backupPath))
        {
            DirAccess.RenameAbsolute(backup, target);
        }

        RestorePreviousBackup(previousBackupPath, previousBackup, backup);
        DirAccess.RemoveAbsolute(temp);
        return false;
    }

    public SaveGame Load()
    {
        foreach (var path in LoadCandidates())
        {
            if (!TryLoad(path, out var save))
            {
                continue;
            }

            if (path != _savePath && Save(save))
            {
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            }
            else if (path == _savePath)
            {
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath($"{_savePath}.bak"));
            }

            return save;
        }

        return new SaveGame();
    }

    public static string BuildSavePath(string? username)
    {
        return $"user://savegame_{AccountIdentity.SaveFileKey(username)}.json";
    }

    public static string BuildLegacyAccountSavePath(string? username)
    {
        var account = AccountIdentity.NormalizeUsername(username);
        var safe = new string(account.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());
        return $"user://savegame_{safe}.json";
    }

    public IReadOnlyList<string> LoadCandidates()
    {
        var candidates = new List<string>
        {
            _savePath,
            $"{_savePath}.bak",
            _legacyAccountSavePath,
            LegacySavePath,
        };
        return candidates.Distinct().ToArray();
    }

    private static bool TryLoad(string path, out SaveGame save)
    {
        save = new SaveGame();
        if (!FileAccess.FileExists(path))
        {
            return false;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"Could not read save file: {FileAccess.GetOpenError()}");
            return false;
        }

        try
        {
            var json = file.GetAsText();
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            save = SaveSerializer.FromJson(json);
            return true;
        }
        catch (Exception error)
        {
            GD.PushWarning($"Could not parse save file {path}: {error.Message}");
            return false;
        }
    }

    private static void RestorePreviousBackup(string previousBackupPath, string previousBackup, string backup)
    {
        if (FileAccess.FileExists(previousBackupPath))
        {
            DirAccess.RenameAbsolute(previousBackup, backup);
        }
    }
}
