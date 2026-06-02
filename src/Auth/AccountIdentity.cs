using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DawnOfBlade.Auth;

/// <summary>Canonical account identity and collision-resistant local-save filename keys.</summary>
public static class AccountIdentity
{
    public static string NormalizeUsername(string? username) =>
        string.IsNullOrWhiteSpace(username) ? "guest" : username.Trim().ToLowerInvariant();

    public static string SaveFileKey(string? username)
    {
        var normalized = NormalizeUsername(username);
        var readable = new string(normalized
            .Where(char.IsLetterOrDigit)
            .Take(20)
            .ToArray());
        if (string.IsNullOrEmpty(readable))
        {
            readable = "account";
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"{readable}_{System.Convert.ToHexString(digest)[..16].ToLowerInvariant()}";
    }
}
