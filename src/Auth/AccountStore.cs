using System;
using System.Security.Cryptography;
using System.Text;
using Godot;
using GDict = Godot.Collections.Dictionary;

namespace DawnOfBlade.Auth;

/// <summary>
/// Stores player accounts locally at <c>user://accounts.json</c> with per-account
/// salted SHA-256 password hashes. There is no remote server in the prototype, so
/// accounts live only on this device.
/// </summary>
public sealed class AccountStore
{
    private const string AccountsPath = "user://accounts.json";

    // Keyed by lowercased username -> { username, email, salt, hash, created }.
    private GDict _accounts = new();

    public AccountStore()
    {
        Load();
    }

    public bool HasAnyAccounts => _accounts.Count > 0;

    public bool Exists(string username) => _accounts.ContainsKey(Key(username));

    /// <summary>Attempts to create a new account. Returns success and a user-facing message.</summary>
    public (bool Ok, string Message) Register(string username, string email, string password, string confirm)
    {
        username = (username ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();
        password ??= string.Empty;

        if (username.Length < 3)
        {
            return (false, "Username must be at least 3 characters.");
        }

        if (password.Length < 6)
        {
            return (false, "Password must be at least 6 characters.");
        }

        if (password != confirm)
        {
            return (false, "Passwords do not match.");
        }

        if (Exists(username))
        {
            return (false, "That username is already taken.");
        }

        var salt = NewSalt();
        _accounts[Key(username)] = new GDict
        {
            ["username"] = username,
            ["email"] = email,
            ["salt"] = salt,
            ["hash"] = Hash(password, salt),
            ["created"] = DateTime.UtcNow.ToString("o"),
        };
        Save();
        return (true, "Account created.");
    }

    /// <summary>Validates a sign-in attempt. Returns success and a user-facing message.</summary>
    public (bool Ok, string Message) Validate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return (false, "Enter your username and password.");
        }

        if (!_accounts.TryGetValue(Key(username), out var value))
        {
            return (false, "No account found with that username.");
        }

        var record = value.AsGodotDictionary();
        var salt = record["salt"].AsString();
        var expected = record["hash"].AsString();
        if (Hash(password, salt) != expected)
        {
            return (false, "Incorrect password.");
        }

        return (true, string.Empty);
    }

    private void Load()
    {
        if (!FileAccess.FileExists(AccountsPath))
        {
            _accounts = new GDict();
            return;
        }

        using var file = FileAccess.Open(AccountsPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning($"Could not open accounts file: {FileAccess.GetOpenError()}");
            _accounts = new GDict();
            return;
        }

        var parsed = Json.ParseString(file.GetAsText());
        _accounts = parsed.VariantType == Variant.Type.Dictionary
            ? parsed.AsGodotDictionary()
            : new GDict();
    }

    private void Save()
    {
        using var file = FileAccess.Open(AccountsPath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushWarning($"Could not write accounts file: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(Json.Stringify(_accounts, "  "));
    }

    private static string Key(string username) => (username ?? string.Empty).Trim().ToLowerInvariant();

    private static string NewSalt() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

    private static string Hash(string password, string salt)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(salt + password));
        return Convert.ToBase64String(digest);
    }
}
