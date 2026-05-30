namespace DawnOfBlade.Auth;

/// <summary>
/// Holds the currently signed-in account for the lifetime of the process so gameplay
/// scenes can read who is playing after the login screen transitions away.
/// </summary>
public static class Session
{
    public static string? Username { get; set; }
    public static string? Server { get; set; }

    public static bool IsSignedIn => !string.IsNullOrEmpty(Username);
}
