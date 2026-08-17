using System.Reflection;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Tests;

public sealed class ProductIdentityTests
{
    [Fact]
    public void ActiveVersion_Is_0_2_4()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        var repositoryRoot = Assert.IsType<DirectoryInfo>(directory);
        var props = File.ReadAllText(Path.Combine(repositoryRoot.FullName, "Directory.Build.props"));

        Assert.Contains("<VersionPrefix>0.2.5</VersionPrefix>", props, StringComparison.Ordinal);
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
    public void Startup_SecondaryInstanceSignalsPrimaryBeforeLoadingGameState()
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
        var coordinatorIndex = startupSource.IndexOf(
            "new DesktopSingleInstanceCoordinator",
            StringComparison.Ordinal);
        var signalIndex = coordinatorIndex < 0
            ? -1
            : startupSource.IndexOf(
                "_singleInstance.SignalPrimary()",
                coordinatorIndex,
                StringComparison.Ordinal);
        var diagnosticIndex = startupSource.IndexOf(
            "InitializeDiagnostics",
            StringComparison.Ordinal);

        Assert.True(coordinatorIndex >= 0);
        Assert.True(signalIndex > coordinatorIndex);
        Assert.True(diagnosticIndex > signalIndex);
        Assert.Contains("ShowDesktopWindow", startupSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationLifetime_HoldsSingleInstanceLeaseUntilFinalSaveAndGuardsCoordinatorStartup()
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
        var startupIndex = startupSource.IndexOf("protected override async void OnStartup", StringComparison.Ordinal);
        var startupTryIndex = startupSource.IndexOf("try", startupIndex, StringComparison.Ordinal);
        var coordinatorIndex = startupSource.IndexOf("new DesktopSingleInstanceCoordinator", startupIndex, StringComparison.Ordinal);
        var exitIndex = startupSource.IndexOf("protected override void OnExit", StringComparison.Ordinal);
        var finalSaveIndex = startupSource.IndexOf("_autosaveCoordinator?.FlushAsync()", exitIndex, StringComparison.Ordinal);
        var disposeIndex = startupSource.IndexOf("_singleInstance?.Dispose()", exitIndex, StringComparison.Ordinal);

        Assert.True(startupIndex >= 0);
        Assert.True(startupTryIndex > startupIndex);
        Assert.True(coordinatorIndex > startupTryIndex);
        Assert.True(finalSaveIndex > exitIndex);
        Assert.True(disposeIndex > finalSaveIndex);
    }

    [Fact]
    public void Startup_WritesInitialSaveBeforeCreatingUiAndStartingCombat()
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
        var sessionStartIndex = startupSource.IndexOf(
            "ReportSessionStart(sessionStart)",
            StringComparison.Ordinal);
        var initialSaveIndex = startupSource.IndexOf(
            "await saveStore.SaveGameAsync(_session.CaptureSaveData())",
            sessionStartIndex,
            StringComparison.Ordinal);
        var viewModelIndex = startupSource.IndexOf(
            "_viewModel = new MarketViewModel",
            sessionStartIndex,
            StringComparison.Ordinal);
        var simulationStartIndex = startupSource.IndexOf(
            "_simulationLoop.Start()",
            sessionStartIndex,
            StringComparison.Ordinal);

        Assert.True(sessionStartIndex >= 0);
        Assert.True(initialSaveIndex > sessionStartIndex);
        Assert.True(viewModelIndex > initialSaveIndex);
        Assert.True(simulationStartIndex > initialSaveIndex);
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
