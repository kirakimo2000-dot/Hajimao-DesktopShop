using System;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Services;

public static class ApplicationDataPathPolicy
{
    public const string OverrideEnvironmentVariable = "HAJIMAO_DATA_DIRECTORY";

    public static string ResolveSavePath(string? explicitDirectory) =>
        Path.Combine(ResolveDataDirectory(explicitDirectory), "hajimao.db");

    public static string ResolveLogDirectory(string? explicitDirectory) =>
        Path.Combine(ResolveDataDirectory(explicitDirectory), "logs");

    private static string ResolveDataDirectory(string? explicitDirectory)
    {
        return string.IsNullOrWhiteSpace(explicitDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HajimaoDesktopShop")
            : Path.GetFullPath(explicitDirectory);
    }
}
