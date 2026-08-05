using System.Xml.Linq;

namespace HajimaoDesktopShop.Release.Tests;

public sealed class ReleasePackagingContractTests
{
    private const string UpgradeCode = "{769C1156-51A9-4D4A-B7A3-3BC5C226B3F2}";
    private readonly RepositoryRoot _root = RepositoryRoot.Locate();

    [Fact]
    public void ActiveVersion_Is_0_1_9()
    {
        var properties = XDocument.Load(_root.File("Directory.Build.props"));
        var version = Assert.Single(
            properties.Descendants(),
            element => element.Name.LocalName == "VersionPrefix");

        Assert.Equal("0.1.9", version.Value.Trim());
    }

    [Fact]
    public void PackagingSources_Exist()
    {
        Assert.True(File.Exists(_root.File("scripts", "build-release.ps1")));
        Assert.True(File.Exists(_root.File("scripts", "test-release.ps1")));
        Assert.True(File.Exists(_root.File(
            "installer",
            "HajimaoDesktopShop.Installer",
            "Package.wxs")));
        Assert.True(File.Exists(_root.File(
            "installer",
            "HajimaoDesktopShop.Installer",
            "HajimaoDesktopShop.Installer.wixproj")));
    }

    [Fact]
    public void WixProject_PinsSdkAndUsesNamedPublishBindPath()
    {
        var project = XDocument.Load(_root.File(
            "installer",
            "HajimaoDesktopShop.Installer",
            "HajimaoDesktopShop.Installer.wixproj"));

        Assert.Equal("WixToolset.Sdk/6.0.2", project.Root?.Attribute("Sdk")?.Value);
        var bindPath = Assert.Single(
            project.Descendants(),
            element => element.Name.LocalName == "BindPath");
        Assert.Equal("$(PublishDir)", bindPath.Attribute("Include")?.Value);
        Assert.Equal("PublishDir", bindPath.Attribute("BindName")?.Value);
        Assert.Contains(
            "ProductVersion=$(ProductVersion)",
            Assert.Single(
                project.Descendants(),
                element => element.Name.LocalName == "DefineConstants").Value,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WixPackage_OwnsApplicationFilesButNotUserData()
    {
        var packagePath = _root.File(
            "installer",
            "HajimaoDesktopShop.Installer",
            "Package.wxs");
        var source = File.ReadAllText(packagePath);
        var packageDocument = XDocument.Parse(source);
        var package = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "Package");

        Assert.Equal("Hajimao DesktopShop", package.Attribute("Name")?.Value);
        Assert.Equal(UpgradeCode, package.Attribute("UpgradeCode")?.Value);
        Assert.Equal("perUserOrMachine", package.Attribute("Scope")?.Value);
        Assert.Equal("yes", package.Attribute("Compressed")?.Value);
        Assert.Single(package.Elements(), element => element.Name.LocalName == "MajorUpgrade");
        Assert.Equal(
            "yes",
            Assert.Single(
                package.Elements(),
                element => element.Name.LocalName == "MediaTemplate").Attribute("EmbedCab")?.Value);

        var mainExecutable = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "File"
                && string.Equals(
                    element.Attribute("Source")?.Value,
                    "!(bindpath.PublishDir)\\HajimaoDesktopShop.Desktop.exe",
                    StringComparison.Ordinal));
        Assert.Single(mainExecutable.Elements(), element => element.Name.LocalName == "Shortcut");

        var nativeSqlite = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "File"
                && string.Equals(
                    element.Attribute("Source")?.Value,
                    "!(bindpath.PublishDir)\\e_sqlite3.dll",
                    StringComparison.Ordinal));
        Assert.Equal("MainExecutable", nativeSqlite.Attribute("CompanionFile")?.Value);
        Assert.Equal(mainExecutable.Parent, nativeSqlite.Parent);
        Assert.True(Guid.TryParse(mainExecutable.Parent?.Attribute("Guid")?.Value, out _));

        var files = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "Files");
        Assert.Equal("!(bindpath.PublishDir)\\**", files.Attribute("Include")?.Value);
        var excludes = files.Elements()
            .Where(element => element.Name.LocalName == "Exclude")
            .Select(element => element.Attribute("Files")?.Value ?? string.Empty)
            .ToArray();
        Assert.Contains(
            excludes,
            exclude => exclude.Contains("HajimaoDesktopShop.Desktop.exe", StringComparison.Ordinal));
        Assert.Contains(excludes, exclude => exclude.EndsWith("e_sqlite3.dll", StringComparison.Ordinal));
        Assert.Contains(excludes, exclude => exclude.Contains("*.pdb", StringComparison.Ordinal));

        Assert.DoesNotContain("LocalAppDataFolder", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hajimao.db", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("logs", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildScript_DefinesVerifiedPortableAndMsiPipeline()
    {
        var script = File.ReadAllText(_root.File("scripts", "build-release.ps1"));

        Assert.Contains("dotnet test", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet publish", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--self-contained true", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-r win-x64", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HajimaoDesktopShop.Installer.wixproj", script, StringComparison.Ordinal);
        Assert.Contains("Compress-Archive", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-FileHash", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConvertTo-Json", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signed = $false", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("schemaVersion = 6", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SmokeScript_IsolatesDataAndOwnsEveryProcessItStops()
    {
        var script = File.ReadAllText(_root.File("scripts", "test-release.ps1"));

        Assert.Contains("[System.IO.Path]::GetTempPath()", script, StringComparison.Ordinal);
        Assert.Contains("[guid]::NewGuid()", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HAJIMAO_DATA_DIRECTORY", script, StringComparison.Ordinal);
        Assert.Contains("Start-Process", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".Id", script, StringComparison.Ordinal);
        Assert.Contains("msiexec.exe", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'/i'", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'/x'", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'/L*v'", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WaitForExit", script, StringComparison.Ordinal);
        Assert.Contains("WS_EX_APPWINDOW", script, StringComparison.Ordinal);
        Assert.Contains("finally", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Get-Process -Name", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process -Name", script, StringComparison.OrdinalIgnoreCase);
    }
}
