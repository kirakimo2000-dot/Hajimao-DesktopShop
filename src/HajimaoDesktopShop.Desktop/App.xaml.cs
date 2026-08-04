using System.IO;
using System.Windows;
using System.Windows.Threading;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Infrastructure.Persistence;

namespace HajimaoDesktopShop.Desktop;

public partial class App : System.Windows.Application
{
    private SimulationLoop? _simulationLoop;
    private DispatcherTimer? _refreshTimer;
    private DispatcherTimer? _autosaveTimer;
    private AutosaveCoordinator? _autosaveCoordinator;
    private BusinessSession? _session;
    private GameSoundService? _soundService;
    private TrayIconService? _trayIconService;
    private MarketViewModel? _viewModel;
    private DesktopShopWindow? _desktopWindow;
    private ManagementWindow? _managementWindow;
    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var catalogPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "products.json");
            var products = await new JsonProductCatalog(catalogPath).LoadAsync();
            var savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HajimaoDesktopShop",
                "hajimao.db");
            var saveStore = new SqliteGameSaveStore(savePath);
            var savedGame = await saveStore.LoadGameAsync();
            var savedPlacement = await saveStore.LoadDesktopWindowPlacementAsync();
            _session = DesktopBusinessSessionFactory.Create(
                products,
                savedGame,
                Environment.TickCount);

            if (savedGame is null)
            {
                var starterStore = _session.Game.GetSnapshot().Stores.Single();
                foreach (var starter in starterStore.Products.Take(3))
                {
                    _session.Game.PlaceProcurementOrder(
                        starterStore.Id,
                        starter.Id,
                        "local-wholesale",
                        Math.Min(5, starter.Capacity));
                }
            }

            _viewModel = new MarketViewModel(
                _session,
                reduceMotion: () => !SystemParameters.ClientAreaAnimation);
            _soundService = new GameSoundService(_viewModel, new SystemGameSoundOutput());
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
            _trayIconService.ExitRequested += OnTrayExitRequested;

            _simulationLoop = new SimulationLoop(_session.Simulation.AdvanceRealSecond);
            _simulationLoop.Start();
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = RefreshCadencePolicy.GetInterval(managementOpen: false)
            };
            _refreshTimer.Tick += OnRefreshTick;
            _refreshTimer.Start();

            _autosaveCoordinator = new AutosaveCoordinator(
                saveStore,
                () => _session.CaptureSaveData(),
                CaptureDesktopWindowPlacement);
            _autosaveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autosaveTimer.Tick += OnAutosaveTick;
            _autosaveTimer.Start();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Hajimao Market 启动失败。\n\n{exception.Message}\n\n请确认 Assets/Config/products.json 完整可读。",
                "Hajimao Market 启动错误",
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
            System.Diagnostics.Debug.WriteLine($"Final autosave failed: {exception}");
        }

        _soundService?.Dispose();
        if (_trayIconService is not null)
        {
            _trayIconService.OpenShopRequested -= OnTrayOpenShopRequested;
            _trayIconService.OpenManagementRequested -= OnOpenManagementRequested;
            _trayIconService.ExitRequested -= OnTrayExitRequested;
            _trayIconService.Dispose();
        }

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

        if (_desktopWindow is not null)
        {
            _desktopWindow.Topmost = false;
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
            _viewModel?.ReportSystemMessage($"自动存档失败：{exception.Message}");
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
        _viewModel?.ReportSystemMessage("小店已隐藏到通知区域，经营仍在继续");
    }

    private void OnTrayOpenShopRequested(object? sender, EventArgs e)
    {
        if (_desktopWindow is not { IsLoaded: true })
        {
            return;
        }

        _desktopWindow.Show();
        _desktopWindow.Activate();
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        _isExiting = true;
        Shutdown();
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

        if (_desktopWindow is { IsLoaded: true })
        {
            _desktopWindow.Topmost = true;
        }
    }
}
