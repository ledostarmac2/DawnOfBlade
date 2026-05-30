using System;
using System.IO;

namespace DawnOfBlade.Tests;

/// <summary>Helpers for reading the real <c>data/*.json</c> content files from the repo during tests.</summary>
internal static class TestContent
{
    public static string DataText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "data", relativePath));

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DawnOfBlade.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate repo root from test output.");
    }
}
