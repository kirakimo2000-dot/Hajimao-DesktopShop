using System;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Services;

public static class ApplicationDataPathPolicy
{
    public const string OverrideEnvironmentVariable = "HAJIMAO_DATA_DIRECTORY";

    public static string ResolveSavePath(string? explicitDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(explicitDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HajimaoDesktopShop")
            : Path.GetFullPath(explicitDirectory);
        return Path.Combine(directory, "hajimao.db");
    }
}
