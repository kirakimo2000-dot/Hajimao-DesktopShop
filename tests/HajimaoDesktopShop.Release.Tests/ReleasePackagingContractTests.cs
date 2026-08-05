using System.Xml.Linq;

namespace HajimaoDesktopShop.Release.Tests;

public sealed class ReleasePackagingContractTests
{
    private const string UpgradeCode = "{769C1156-51A9-4D4A-B7A3-3BC5C226B3F2}";
    private readonly RepositoryRoot _root = RepositoryRoot.Locate();

    [Fact]
    public void ActiveVersion_Is_0_1_10()
    {
        var properties = XDocument.Load(_root.File("Directory.Build.props"));
        var version = Assert.Single(
            properties.Descendants(),
            element => element.Name.LocalName == "VersionPrefix");

        Assert.Equal("0.1.10", version.Value.Trim());
    }

    [Fact]
    public void PackagingSources_Exist()
    {
        Assert.True(File.Exists(_root.File("scripts", "build-release.ps1")));
        Assert.True(File.Exists(_root.File("scripts", "test-release.ps1")));
        Assert.True(File.Exists(_root.File(".github", "workflows", "release-gate.yml")));
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
        Assert.Equal("perMachine", package.Attribute("Scope")?.Value);
        Assert.Equal("yes", package.Attribute("Compressed")?.Value);
        Assert.Single(package.Elements(), element => element.Name.LocalName == "MajorUpgrade");
        Assert.Equal(
            "yes",
            Assert.Single(
                package.Elements(),
                element => element.Name.LocalName == "MediaTemplate").Attribute("EmbedCab")?.Value);

        var installFolder = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "Directory"
                && string.Equals(element.Attribute("Id")?.Value, "INSTALLFOLDER", StringComparison.Ordinal));
        Assert.Equal("ProgramFiles6432Folder", installFolder.Parent?.Attribute("Id")?.Value);

        var installLocationProperty = Assert.Single(
            packageDocument.Descendants(),
            element => element.Name.LocalName == "SetProperty"
                && string.Equals(
                    element.Attribute("Id")?.Value,
                    "ARPINSTALLLOCATION",
                    StringComparison.Ordinal));
        Assert.Equal("[INSTALLFOLDER]", installLocationProperty.Attribute("Value")?.Value);
        Assert.Equal("CostFinalize", installLocationProperty.Attribute("After")?.Value);

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

        Assert.DoesNotContain("hajimao.db", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("logs", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LocalAppDataFolder", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildScript_DefinesVerifiedPortableAndMsiPipeline()
    {
        var script = File.ReadAllText(_root.File("scripts", "build-release.ps1"));

        Assert.Contains("dotnet test", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet publish", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--self-contained true", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "IncludeSourceRevisionInInformationalVersion=false",
            script,
            StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Get-MsiProperty", script, StringComparison.Ordinal);
        Assert.Contains("ProductsEx", script, StringComparison.Ordinal);
        Assert.Contains("InstallProperty('InstallLocation')", script, StringComparison.Ordinal);
        Assert.Contains("$ownedProductCode", script, StringComparison.Ordinal);
        Assert.Contains("ProcessStartInfo", script, StringComparison.Ordinal);
        Assert.Contains("ArgumentList.Add", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-CleanupStep", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-Process -Name", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process -Name", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$msiInstalled", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ALLUSERS=2", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MSIINSTALLPERUSER", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALLUSERS=1", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'/a'", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RequireFullMsiInstall", script, StringComparison.Ordinal);
        Assert.Contains("WindowsBuiltInRole", script, StringComparison.Ordinal);

        var installCompleted = script.IndexOf(
            "Invoke-MsiExec -Operation 'MSI install'",
            StringComparison.Ordinal);
        var ownershipRecorded = script.IndexOf(
            "$ownedProductCode = $productCode",
            installCompleted,
            StringComparison.Ordinal);
        var registrationValidation = script.IndexOf(
            "$installedProducts =",
            installCompleted,
            StringComparison.Ordinal);
        Assert.True(installCompleted >= 0);
        Assert.True(ownershipRecorded > installCompleted);
        Assert.True(registrationValidation > ownershipRecorded);
    }

    [Fact]
    public void ReleaseWorkflow_RequiresFullPerMachineMsiSmoke()
    {
        var workflow = File.ReadAllText(
            _root.File(".github", "workflows", "release-gate.yml"));

        Assert.Contains("windows-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", workflow, StringComparison.Ordinal);
        Assert.Contains("build-release.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("-RequireFullMsiInstall", workflow, StringComparison.Ordinal);
    }
}
