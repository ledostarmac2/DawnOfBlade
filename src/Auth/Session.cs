namespace DawnOfBlade.Auth;

/// <summary>
/// Holds the currently signed-in account for the lifetime of the process so gameplay
/// scenes can read who is playing after the login screen transitions away.
/// </summary>
public static class Session
{
    private static string? _username;

    public static string? Username
    {
        get => _username;
        set => _username = string.IsNullOrWhiteSpace(value) ? null : AccountIdentity.NormalizeUsername(value);
    }

    public static bool IsSignedIn => !string.IsNullOrEmpty(Username);
}
