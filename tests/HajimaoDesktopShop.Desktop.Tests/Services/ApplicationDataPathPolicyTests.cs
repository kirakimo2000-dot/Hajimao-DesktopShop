using System.IO;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class ApplicationDataPathPolicyTests
{
    [Fact]
    public void ResolveSavePath_UsesExplicitDirectoryForIsolatedSmokeProfiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "hajimao-isolated-profile");

        var path = ApplicationDataPathPolicy.ResolveSavePath(directory);

        Assert.Equal(Path.Combine(Path.GetFullPath(directory), "hajimao.db"), path);
    }

    [Fact]
    public void ResolveSavePath_DefaultsToTheProductLocalApplicationDataFolder()
    {
        var path = ApplicationDataPathPolicy.ResolveSavePath(null);

        Assert.Equal(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HajimaoDesktopShop",
                "hajimao.db"),
            path);
    }
}
