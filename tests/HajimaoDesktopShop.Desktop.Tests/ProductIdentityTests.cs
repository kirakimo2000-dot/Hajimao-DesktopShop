using System.Reflection;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Tests;

public sealed class ProductIdentityTests
{
    [Fact]
    public void ActiveVersion_Is_0_2_0()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        var repositoryRoot = Assert.IsType<DirectoryInfo>(directory);
        var props = File.ReadAllText(Path.Combine(repositoryRoot.FullName, "Directory.Build.props"));

        Assert.Contains("<VersionPrefix>0.2.0</VersionPrefix>", props, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductIdentity_UsesOfficialDesktopShopBrandAcrossVisibleSurfaces()
    {
        Assert.Equal("Hajimao DesktopShop", ProductIdentity.DisplayName);
        Assert.Equal("HAJIMAO DESKTOPSHOP", ProductIdentity.BrandHeader);
        Assert.Equal("Hajimao DesktopShop · 挂机街区", ProductIdentity.DesktopWindowTitle);
        Assert.Equal("Hajimao DesktopShop · 战斗管理", ProductIdentity.ManagementWindowTitle);
        Assert.Equal("Hajimao DesktopShop · 挂机战斗中", ProductIdentity.TrayTooltip);
        Assert.Equal("退出 Hajimao DesktopShop", ProductIdentity.ExitMenuText);
        Assert.Equal("Hajimao DesktopShop 启动错误", ProductIdentity.StartupErrorTitle);
        Assert.Equal(
            "你已经掌握 Hajimao DesktopShop 的核心挂机战斗。",
            ProductIdentity.OnboardingCompletionGuidance);
    }

    [Fact]
    public void DesktopAssemblyMetadata_UsesOfficialDisplayName()
    {
        var assembly = typeof(ProductIdentity).Assembly;

        Assert.Equal(
            ProductIdentity.DisplayName,
            assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);
        Assert.Equal(
            ProductIdentity.DisplayName,
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
    }

    [Fact]
    public void Startup_WritesDiagnosticBeforeWaitingForStarterStoreChoice()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        var repositoryRoot = Assert.IsType<DirectoryInfo>(directory);
        var startupSource = File.ReadAllText(Path.Combine(
            repositoryRoot.FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "App.xaml.cs"));
        var diagnosticIndex = startupSource.IndexOf(
            "\"application.starting\"",
            StringComparison.Ordinal);
        var selectionIndex = startupSource.IndexOf(
            "StarterStoreStartupCoordinator.SelectForStartup",
            StringComparison.Ordinal);

        Assert.True(diagnosticIndex >= 0);
        Assert.True(selectionIndex > diagnosticIndex);
    }

    [Fact]
    public void StartupFailure_MessageDoesNotMisdiagnoseEveryFailureAsProductCatalogDamage()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        var repositoryRoot = Assert.IsType<DirectoryInfo>(directory);
        var startupSource = File.ReadAllText(Path.Combine(
            repositoryRoot.FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "App.xaml.cs"));

        Assert.DoesNotContain(
            "请确认 Assets/Config/products.json 完整可读。",
            startupSource,
            StringComparison.Ordinal);
        Assert.Contains("诊断日志", startupSource, StringComparison.Ordinal);
    }
}
