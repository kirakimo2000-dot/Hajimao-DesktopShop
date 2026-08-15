using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Diagnostics.Export;
using HajimaoDesktopShop.Application.Diagnostics;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Infrastructure.Diagnostics.Export;
using HajimaoDesktopShop.Infrastructure.Logging;
using HajimaoDesktopShop.Infrastructure.Persistence;

namespace HajimaoDesktopShop.Desktop;

public partial class App : System.Windows.Application
{
    private SimulationLoop? _simulationLoop;
    private DispatcherTimer? _refreshTimer;
    private DispatcherTimer? _autosaveTimer;
    private AutosaveCoordinator? _autosaveCoordinator;
    private BusinessSession? _session;
    private TrayIconService? _trayIconService;
    private MarketViewModel? _viewModel;
    private DesktopShopWindow? _desktopWindow;
    private ManagementWindow? _managementWindow;
    private IGameDiagnosticSink _diagnosticSink = NullGameDiagnosticSink.Instance;
    private IDisposable? _diagnosticLifetime;
    private string? _dataDirectoryOverride;
    private bool _startupCompleted;
    private bool _isExiting;
    private bool _isExportingFeedback;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var dataDirectoryOverride = Environment.GetEnvironmentVariable(
                ApplicationDataPathPolicy.OverrideEnvironmentVariable);
            _dataDirectoryOverride = dataDirectoryOverride;
            InitializeDiagnostics(dataDirectoryOverride);
            ReportDiagnostic(
                "application.starting",
                GameDiagnosticLevel.Information,
                "Application startup initialized.");

            var catalogPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "products.json");
            var products = await new JsonProductCatalog(catalogPath).LoadAsync();
            var storeFormatsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Config",
                "store-formats.json");
            var storeBrandsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Config",
                "store-brands.json");
            var storeContent = await new JsonStoreContentCatalog(
                storeFormatsPath,
                storeBrandsPath).LoadAsync();
            var combatContent = await new JsonCombatContentCatalog(
                catalogPath,
                storeBrandsPath,
                Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "product-combat.json"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "customers", "customer-archetypes.json"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "customers", "customer-spawn-pools.json"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "characters", "characters.json"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "interiors", "interiors.json"))
                .LoadAsync();
            var savePath = ApplicationDataPathPolicy.ResolveSavePath(dataDirectoryOverride);
            var saveStore = new SqliteGameSaveStore(savePath);
            var savedGame = await saveStore.LoadGameAsync();
            var savedPlacement = await saveStore.LoadDesktopWindowPlacementAsync();
            var startupUtc = DateTimeOffset.UtcNow;
            var starterStore = StarterStoreStartupCoordinator.SelectForStartup(
                savedGame,
                storeContent,
                Environment.TickCount,
                viewModel => new StarterStoreChoiceWindow(viewModel).ShowDialog());
            if (!starterStore.ShouldContinue)
            {
                Shutdown();
                return;
            }

            var sessionStart = DesktopBusinessSessionFactory.Create(
                products,
                savedGame,
                Environment.TickCount,
                startupUtc,
                storeContent: storeContent,
                peopleMarketContent: null,
                starterStoreProposal: starterStore.Proposal,
                combatContent: combatContent);
            _session = sessionStart.Session;
            ReportSessionStart(sessionStart);
            await saveStore.SaveGameAsync(_session.CaptureSaveData());
            ReportDiagnostic(
                "persistence.initial_save.completed",
                GameDiagnosticLevel.Information,
                "Initial game state saved before desktop UI initialization.");
            _viewModel = new MarketViewModel(
                _session,
                reduceMotion: () => !SystemParameters.ClientAreaAnimation);
            if (savedPlacement is not null)
            {
                _viewModel.RestoreDesktopState(savedPlacement.IsLocked);
            }

            _desktopWindow = new DesktopShopWindow(_viewModel);
            _desktopWindow.OpenManagementRequested += OnOpenManagementRequested;
            _desktopWindow.Closing += OnDesktopWindowClosing;
            _desktopWindow.Closed += OnDesktopWindowClosed;
            MainWindow = _desktopWindow;
            _desktopWindow.Show();
            if (savedPlacement is null
                || !WindowInteractionService.TryRestorePlacement(_desktopWindow, savedPlacement))
            {
                WindowInteractionService.SnapToNearestWorkAreaCorner(_desktopWindow);
            }

            _trayIconService = new TrayIconService();
            _trayIconService.OpenShopRequested += OnTrayOpenShopRequested;
            _trayIconService.OpenManagementRequested += OnOpenManagementRequested;
            _trayIconService.ExportFeedbackRequested += OnTrayExportFeedbackRequested;
            _trayIconService.ExitRequested += OnTrayExitRequested;

            _autosaveCoordinator = new AutosaveCoordinator(
                saveStore,
                () => _session.CaptureSaveData(),
                CaptureDesktopWindowPlacement);

            _simulationLoop = new SimulationLoop(
                () => _session.AdvanceCombatRealSecond(DateTimeOffset.Now.Hour),
                reportFailure: ReportSimulationFailure);
            _simulationLoop.Start();
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = RefreshCadencePolicy.GetInterval(managementOpen: false)
            };
            _refreshTimer.Tick += OnRefreshTick;
            _refreshTimer.Start();

            _autosaveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autosaveTimer.Tick += OnAutosaveTick;
            _autosaveTimer.Start();
            _startupCompleted = true;
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "application.startup.failed",
                GameDiagnosticLevel.Error,
                "Application startup failed.",
                exception: exception);
            MessageBox.Show(
                $"{ProductIdentity.DisplayName} 启动失败。\n\n{exception.Message}\n\n请保留此错误截图；诊断日志位于本机应用数据目录的 logs 文件夹。",
                ProductIdentity.StartupErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _refreshTimer?.Stop();
        if (_refreshTimer is not null)
        {
            _refreshTimer.Tick -= OnRefreshTick;
        }

        _autosaveTimer?.Stop();
        if (_autosaveTimer is not null)
        {
            _autosaveTimer.Tick -= OnAutosaveTick;
        }

        _simulationLoop?.StopAsync().GetAwaiter().GetResult();
        try
        {
            _autosaveCoordinator?.FlushAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "persistence.final_save.failed",
                GameDiagnosticLevel.Error,
                "Final save failed during shutdown.",
                exception: exception);
        }

        if (_trayIconService is not null)
        {
            _trayIconService.OpenShopRequested -= OnTrayOpenShopRequested;
            _trayIconService.OpenManagementRequested -= OnOpenManagementRequested;
            _trayIconService.ExportFeedbackRequested -= OnTrayExportFeedbackRequested;
            _trayIconService.ExitRequested -= OnTrayExitRequested;
            _trayIconService.Dispose();
        }

        if (_startupCompleted)
        {
            ReportDiagnostic(
                "application.stopped",
                GameDiagnosticLevel.Information,
                "Application stopped normally.");
        }

        _diagnosticLifetime?.Dispose();
        _diagnosticLifetime = null;
        _diagnosticSink = NullGameDiagnosticSink.Instance;
        base.OnExit(e);
    }

    private void OnOpenManagementRequested(object? sender, EventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_managementWindow is null || !_managementWindow.IsLoaded)
        {
            _managementWindow = new ManagementWindow(_viewModel);
            _managementWindow.Closed += OnManagementWindowClosed;
        }

        _managementWindow.Show();
        _managementWindow.Activate();
        if (_refreshTimer is not null)
        {
            _refreshTimer.Interval = RefreshCadencePolicy.GetInterval(managementOpen: true);
        }
    }

    private void OnRefreshTick(object? sender, EventArgs e) => _viewModel?.Refresh();

    private async void OnAutosaveTick(object? sender, EventArgs e)
    {
        if (_autosaveCoordinator is null)
        {
            return;
        }

        try
        {
            await _autosaveCoordinator.TryAutosaveAsync();
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "persistence.autosave.failed",
                GameDiagnosticLevel.Error,
                "Autosave failed.",
                exception: exception);
        }
    }

    private DesktopWindowPlacement? CaptureDesktopWindowPlacement() =>
        _desktopWindow is null
            ? null
            : new DesktopWindowPlacement(
                _desktopWindow.Left,
                _desktopWindow.Top,
                _viewModel?.IsLocked ?? false);

    private void OnDesktopWindowClosed(object? sender, EventArgs e)
    {
        if (sender is DesktopShopWindow window)
        {
            window.OpenManagementRequested -= OnOpenManagementRequested;
            window.Closing -= OnDesktopWindowClosing;
            window.Closed -= OnDesktopWindowClosed;
        }

        _managementWindow?.Close();
    }

    private void OnDesktopWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting || sender is not DesktopShopWindow window)
        {
            return;
        }

        e.Cancel = true;
        window.Hide();
    }

    private void OnTrayOpenShopRequested(object? sender, EventArgs e)
    {
        if (_desktopWindow is not { IsLoaded: true })
        {
            return;
        }

        _viewModel?.DesktopNavigation.ShowStreet();
        _desktopWindow.Show();
        _desktopWindow.Activate();
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        _isExiting = true;
        Shutdown();
    }

    private async void OnTrayExportFeedbackRequested(object? sender, EventArgs e)
    {
        if (_isExportingFeedback)
        {
            return;
        }

        if (_session is null || _autosaveCoordinator is null)
        {
            ReportFeedbackExportFailed("Unavailable");
            MessageBox.Show(
                "当前会话尚未准备好，无法生成测试反馈包。",
                ProductIdentity.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _isExportingFeedback = true;
        try
        {
            await _autosaveCoordinator.FlushAsync();
            var snapshot = _session.Simulation.GetSnapshot();
            var diagnosticEvents = SanitizedDiagnosticLogReader.Read(
                ApplicationDataPathPolicy.ResolveLogDirectory(_dataDirectoryOverride),
                maximumEvents: 200);
            var createdAtUtc = DateTimeOffset.UtcNow;
            var report = PlaytestFeedbackReportFactory.Create(
                snapshot,
                diagnosticEvents,
                GetInformationalVersion(),
                createdAtUtc);
            var destinationPath = PlaytestFeedbackArchiveWriter.Write(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                report);

            ReportDiagnostic(
                "feedback.export.completed",
                GameDiagnosticLevel.Information,
                "Feedback export completed.",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["OpenStoreCount"] = report.OpenStoreCount.ToString(CultureInfo.InvariantCulture),
                    ["EmployeeCount"] = report.EmployeeCount.ToString(CultureInfo.InvariantCulture),
                    ["DiagnosticEventCount"] = report.DiagnosticEvents.Count.ToString(CultureInfo.InvariantCulture)
                });
            MessageBox.Show(
                $"测试反馈包已生成：\n\n{destinationPath}",
                ProductIdentity.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ReportFeedbackExportFailed(exception.GetType().Name);
            MessageBox.Show(
                "生成测试反馈包失败，请稍后重试。",
                ProductIdentity.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isExportingFeedback = false;
        }
    }

    private void OnManagementWindowClosed(object? sender, EventArgs e)
    {
        if (sender is ManagementWindow managementWindow)
        {
            managementWindow.Closed -= OnManagementWindowClosed;
        }

        _managementWindow = null;
        if (_refreshTimer is not null)
        {
            _refreshTimer.Interval = RefreshCadencePolicy.GetInterval(managementOpen: false);
        }

    }

    private void ReportSessionStart(DesktopBusinessSessionStartResult sessionStart)
    {
        ReportDiagnostic(
            "application.started",
            GameDiagnosticLevel.Information,
            "Application session started.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Mode"] = sessionStart.IsNewGame ? "new" : "restored",
                ["StoreCount"] = sessionStart.Session.Game.GetSnapshot().Stores.Count
                    .ToString(CultureInfo.InvariantCulture)
            });

    }

    private void ReportSimulationFailure(Exception exception) =>
        ReportDiagnostic(
            "simulation.failed",
            GameDiagnosticLevel.Error,
            "Simulation loop stopped after an unexpected failure.",
            exception: exception);

    private void ReportFeedbackExportFailed(string failureType) =>
        ReportDiagnostic(
            "feedback.export.failed",
            GameDiagnosticLevel.Error,
            "Feedback export failed.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["FailureType"] = failureType
            });

    private static string GetInformationalVersion() =>
        typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(App).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private void InitializeDiagnostics(string? dataDirectoryOverride)
    {
        try
        {
            var fileDiagnosticSink = new SerilogGameDiagnosticSink(
                ApplicationDataPathPolicy.ResolveLogDirectory(dataDirectoryOverride));
            _diagnosticSink = fileDiagnosticSink;
            _diagnosticLifetime = fileDiagnosticSink;
        }
        catch
        {
            _diagnosticSink = NullGameDiagnosticSink.Instance;
            _diagnosticLifetime = null;
        }
    }

    private void ReportDiagnostic(
        string name,
        GameDiagnosticLevel level,
        string message,
        IReadOnlyDictionary<string, string>? properties = null,
        Exception? exception = null)
    {
        try
        {
            _diagnosticSink.Write(new GameDiagnosticEvent(
                name,
                level,
                message,
                properties,
                exception));
        }
        catch
        {
            // Diagnostics must never prevent simulation, saving, or shutdown.
        }
    }
}
