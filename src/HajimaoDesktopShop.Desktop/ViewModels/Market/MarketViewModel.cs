using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<bool> _reduceMotion;
    private ManagementSection _selectedSection = ManagementSection.Overview;
    private string _selectedStoreId = string.Empty;
    private string _selectedStoreName = string.Empty;
    private string _cashText = "¥0.00";
    private string _playerLevelText = "Lv.1";
    private string _gameTimeText = "第 1 天 00:00";
    private string _stockWarningText = "缺货/低库存 0";
    private string _customerCountText = "顾客/队列 0";
    private string _statusMessage = "小店准备营业";
    private bool _isLocked;
    private bool _isClickThrough;
    private bool _isMuted;
    private bool _isStatusBarExpanded = true;
    private int _lastCompletedSales;
    private int _animationFrame;
    private BusinessShopSceneFrame? _sceneFrame;
    private BusinessShopFrame? _desktopFrame;
    private BusinessShopInteractionTarget? _selectedShopTarget;
    private ShopObjectDetailViewModel? _selectedShopObject;

    public MarketViewModel(BusinessSession session, Func<bool>? reduceMotion = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _reduceMotion = reduceMotion ?? (() => false);
        NavigateCommand = new RelayCommand<ManagementSection>(Navigate);
        GoToOnboardingTaskCommand = new RelayCommand(GoToOnboardingTask);
        SelectStoreCommand = new RelayCommand<StoreNavigationItemViewModel>(SelectStore);
        SelectShopObjectCommand = new RelayCommand<BusinessShopInteractionTarget>(SelectShopObject);
        ToggleLockCommand = new RelayCommand(ToggleLock);
        ToggleClickThroughCommand = new RelayCommand(ToggleClickThrough);
        ToggleMuteCommand = new RelayCommand(ToggleMute);
        ToggleStatusBarCommand = new RelayCommand(ToggleStatusBar);
        DesktopNavigation = new DesktopNavigationViewModel(SelectStoreById);
        Onboarding = new OnboardingViewModel();
        Overview = new MarketOverviewViewModel();
        Economy = new StoreEconomyViewModel();
        Progression = new LongTermProgressionViewModel();
        Strategy = new StoreStrategyViewModel(session, () => SelectedStoreId);
        Investment = new InvestmentPortfolioViewModel(session, () => SelectedStoreId, Refresh);
        CommercialStreet = new CommercialStreetViewModel();
        Strategy.FeedbackRaised += RelayFeedback;
        Investment.FeedbackRaised += RelayFeedback;
        Refresh();
    }

    public ObservableCollection<StoreNavigationItemViewModel> Stores { get; } = [];

    public MarketOverviewViewModel Overview { get; }

    public StoreEconomyViewModel Economy { get; }

    public LongTermProgressionViewModel Progression { get; }

    public StoreStrategyViewModel Strategy { get; }

    public InvestmentPortfolioViewModel Investment { get; }

    public CommercialStreetViewModel CommercialStreet { get; }

    public OnboardingViewModel Onboarding { get; }

    public DesktopNavigationViewModel DesktopNavigation { get; }

    public IRelayCommand<ManagementSection> NavigateCommand { get; }

    public IRelayCommand GoToOnboardingTaskCommand { get; }

    public IRelayCommand<StoreNavigationItemViewModel> SelectStoreCommand { get; }

    public IRelayCommand<BusinessShopInteractionTarget> SelectShopObjectCommand { get; }

    public IRelayCommand ToggleLockCommand { get; }

    public IRelayCommand ToggleClickThroughCommand { get; }

    public IRelayCommand ToggleMuteCommand { get; }

    public IRelayCommand ToggleStatusBarCommand { get; }

    public event EventHandler<GameFeedbackEventArgs>? FeedbackRaised;

    public string TimeModeText => "固定现实 1x";

    public ManagementSection SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (!SetProperty(ref _selectedSection, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsOverviewSection));
            OnPropertyChanged(nameof(IsStrategySection));
            OnPropertyChanged(nameof(IsInvestmentSection));
        }
    }

    public bool IsOverviewSection => SelectedSection == ManagementSection.Overview;
    public bool IsStrategySection => SelectedSection == ManagementSection.Strategy;
    public bool IsInvestmentSection => SelectedSection == ManagementSection.Investment;

    public string SelectedStoreId
    {
        get => _selectedStoreId;
        private set => SetProperty(ref _selectedStoreId, value);
    }

    public string SelectedStoreName
    {
        get => _selectedStoreName;
        private set => SetProperty(ref _selectedStoreName, value);
    }

    public string CashText
    {
        get => _cashText;
        private set => SetProperty(ref _cashText, value);
    }

    public string PlayerLevelText
    {
        get => _playerLevelText;
        private set => SetProperty(ref _playerLevelText, value);
    }

    public string GameTimeText
    {
        get => _gameTimeText;
        private set => SetProperty(ref _gameTimeText, value);
    }

    public string StockWarningText
    {
        get => _stockWarningText;
        private set => SetProperty(ref _stockWarningText, value);
    }

    public string CustomerCountText
    {
        get => _customerCountText;
        private set => SetProperty(ref _customerCountText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        private set => SetProperty(ref _isLocked, value);
    }

    public bool IsClickThrough
    {
        get => _isClickThrough;
        private set => SetProperty(ref _isClickThrough, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        private set
        {
            if (SetProperty(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(SoundToggleText));
            }
        }
    }

    public string SoundToggleText => IsMuted ? "开启音效" : "静音";

    public bool IsStatusBarExpanded
    {
        get => _isStatusBarExpanded;
        private set
        {
            if (SetProperty(ref _isStatusBarExpanded, value))
            {
                OnPropertyChanged(nameof(StatusBarHeight));
                OnPropertyChanged(nameof(StatusBarToggleText));
            }
        }
    }

    public double StatusBarHeight => IsStatusBarExpanded ? 56d : 34d;

    public string StatusBarToggleText => IsStatusBarExpanded ? "收起状态栏" : "展开状态栏";

    public BusinessShopSceneFrame? SceneFrame
    {
        get => _sceneFrame;
        private set => SetProperty(ref _sceneFrame, value);
    }

    public BusinessShopFrame? DesktopFrame
    {
        get => _desktopFrame;
        private set => SetProperty(ref _desktopFrame, value);
    }

    public ShopObjectDetailViewModel? SelectedShopObject
    {
        get => _selectedShopObject;
        private set
        {
            if (SetProperty(ref _selectedShopObject, value))
            {
                OnPropertyChanged(nameof(HasSelectedShopObject));
            }
        }
    }

    public bool HasSelectedShopObject => SelectedShopObject is not null;

    public void Refresh()
    {
        var snapshot = _session.Simulation.GetSnapshot();
        Onboarding.Refresh(OnboardingProgressService.CreateSnapshot(
            snapshot,
            _session.Game.GetProcurementSnapshot(),
            _session.Investments.HasAnyInvestment));
        var completedSales = snapshot.Stores.Sum(item => item.CompletedSales);
        if (completedSales > _lastCompletedSales)
        {
            _lastCompletedSales = completedSales;
            FeedbackRaised?.Invoke(this, new GameFeedbackEventArgs(GameFeedbackKind.SaleCompleted));
        }
        var storeCatalog = _session.Game.GetStoreCatalogSnapshot();
        SynchronizeStores(storeCatalog);

        if (string.IsNullOrEmpty(SelectedStoreId))
        {
            var firstOpenStore = Stores.First(store => store.IsOpen);
            SelectedStoreId = firstOpenStore.Id;
            SelectedStoreName = firstOpenStore.Name;
        }
        else
        {
            SelectedStoreName = Stores.Single(store => store.Id == SelectedStoreId).Name;
        }

        DesktopNavigation.Synchronize(Stores, SelectedStoreId);

        CashText = FormatMoney(snapshot.Business.CashCents);
        PlayerLevelText = $"Lv.{snapshot.Business.PlayerLevel}";
        GameTimeText = FormatGameTime(snapshot.GameMinute);

        var store = snapshot.Business.Stores.SingleOrDefault(item => item.Id == SelectedStoreId);
        var operations = snapshot.Stores.SingleOrDefault(item => item.StoreId == SelectedStoreId);
        var warningCount = store?.Products.Count(product =>
            product.Quantity == 0 || product.Quantity * 4 < product.Capacity) ?? 0;
        StockWarningText = $"缺货/低库存 {warningCount}";
        CustomerCountText = $"顾客/队列 {operations?.CheckoutQueueLength ?? 0}";
        var reduceMotion = _reduceMotion();
        SceneFrame = new BusinessShopSceneFrame(
            snapshot,
            SelectedStoreId,
            reduceMotion ? 0 : _animationFrame,
            reduceMotion);
        CommercialStreet.Refresh(snapshot.Street, SceneFrame.AnimationFrame, reduceMotion);
        if (!reduceMotion)
        {
            _animationFrame = _animationFrame == int.MaxValue ? 0 : _animationFrame + 1;
        }
        DesktopFrame = new BusinessShopFrame(
            SceneFrame,
            CashText,
            PlayerLevelText,
            GameTimeText,
            StockWarningText,
            CustomerCountText,
            IsLocked,
            IsClickThrough);
        RefreshSelectedShopObject(snapshot);
        Overview.Synchronize(Stores);
        var analysis = StoreEconomyAnalysisService.Calculate(snapshot, SelectedStoreId);
        if (analysis is not null)
        {
            Economy.Update(analysis);
        }

        Progression.Update(
            LongTermProgressionService.Create(
                snapshot,
                storeCatalog,
                snapshot.Business.Stores
                    .Select(store => _session.Game.GetStoreGrowthSnapshot(store.Id))
                    .ToArray(),
                _session.Investments.HasAnyInvestment),
            storeCatalog);

        Strategy.Refresh();
        Investment.Refresh();
    }

    public void RestoreDesktopState(bool isLocked)
    {
        IsLocked = isLocked;
        IsClickThrough = false;
        Refresh();
    }

    public void ReportSystemMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            StatusMessage = message.Trim();
        }
    }

    private void Navigate(ManagementSection section) => SelectedSection = section;

    private void GoToOnboardingTask() => Navigate(Onboarding.SuggestedSection);

    private void ToggleLock()
    {
        IsLocked = !IsLocked;
        StatusMessage = IsLocked ? "桌面小店已锁定" : "桌面小店可拖动";
        Refresh();
    }

    private void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;
        StatusMessage = IsClickThrough ? "鼠标穿透已开启" : "鼠标穿透已关闭";
        Refresh();
    }

    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        StatusMessage = IsMuted ? "音效已静音" : "音效已开启";
    }

    private void ToggleStatusBar() => IsStatusBarExpanded = !IsStatusBarExpanded;

    private void RelayFeedback(object? sender, GameFeedbackEventArgs e) =>
        FeedbackRaised?.Invoke(this, e);

    private void SelectStore(StoreNavigationItemViewModel? store)
    {
        if (store is null)
        {
            return;
        }

        SelectedStoreId = store.Id;
        SelectedStoreName = store.Name;
        _selectedShopTarget = null;
        SelectedShopObject = null;
        Refresh();
    }

    private void SelectShopObject(BusinessShopInteractionTarget? target)
    {
        if (target is null || SceneFrame is null)
        {
            return;
        }

        _selectedShopTarget = target;
        SelectedSection = ManagementSection.Overview;
        RefreshSelectedShopObject(SceneFrame.Snapshot);
    }

    private void RefreshSelectedShopObject(BusinessSimulationSnapshot snapshot)
    {
        if (_selectedShopTarget is null)
        {
            SelectedShopObject = null;
            return;
        }

        SelectedShopObject = _selectedShopTarget.Kind switch
        {
            BusinessShopInteractionKind.Shelf => CreateShelfDetail(snapshot, _selectedShopTarget.Key),
            BusinessShopInteractionKind.Employee => CreateEmployeeDetail(snapshot, _selectedShopTarget.Key),
            _ => null
        };
        if (SelectedShopObject is null)
        {
            _selectedShopTarget = null;
        }
    }

    private ShopObjectDetailViewModel? CreateShelfDetail(
        BusinessSimulationSnapshot snapshot,
        string shelfKind)
    {
        var store = snapshot.Business.Stores.SingleOrDefault(item => item.Id == SelectedStoreId);
        if (store is null)
        {
            return null;
        }

        var products = store.Products
            .Where(product => string.Equals(
                product.ShelfKind,
                shelfKind,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var title = shelfKind.ToLowerInvariant() switch
        {
            "chilled" => "冷藏货架",
            "frozen" => "冷冻货架",
            _ => "常温货架"
        };
        var quantity = products.Sum(product => product.Quantity);
        var capacity = products.Sum(product => product.Capacity);
        var outOfStock = products.Count(product => product.Quantity == 0);
        var lowStock = products.Count(product =>
            product.Quantity > 0 && product.Quantity * 4 < product.Capacity);
        var averageMargin = products.Length == 0
            ? 0
            : (long)Math.Round(products.Average(product => product.UnitGrossProfitCents));
        var actionTarget = ShelfActionTargetSelector.Select(products, shelfKind);
        var isAutoRestockEnabled = actionTarget is not null
            && _session.Game.GetProcurementSnapshot().AutoRestockPolicies.Any(policy =>
                policy.StoreId == SelectedStoreId
                && policy.ProductId == actionTarget.Id
                && policy.IsEnabled);
        return new ShopObjectDetailViewModel(
            BusinessShopInteractionKind.Shelf,
            shelfKind,
            title,
            "商品与库存",
            $"SKU {products.Length} · 库存 {quantity}/{capacity}",
            $"缺货 {outOfStock} · 低库存 {lowStock} · 平均毛利 {FormatMoney(averageMargin)}",
            actionTarget?.Id ?? string.Empty,
            actionTarget is null
                ? "当前货架没有可经营商品"
                : $"系统按整店策略管理 {actionTarget.Name} · 当前 {actionTarget.Quantity}/{actionTarget.Capacity}",
            isAutoRestockEnabled);
    }

    private ShopObjectDetailViewModel? CreateEmployeeDetail(
        BusinessSimulationSnapshot snapshot,
        string employeeId)
    {
        var employee = snapshot.Employees.Employees.SingleOrDefault(item =>
            item.EmployeeId == employeeId && item.StoreId == SelectedStoreId);
        if (employee is null)
        {
            return null;
        }

        var role = employee.Role switch
        {
            EmployeeRole.Cashier => "收银员",
            EmployeeRole.Restocker => "补货员",
            EmployeeRole.SalesAssistant => "导购员",
            EmployeeRole.Cleaner => "清洁员",
            EmployeeRole.Manager => "店长",
            EmployeeRole.Buyer => "采购员",
            _ => employee.Role.ToString()
        };
        var shift = employee.IsAlwaysOn
            ? "全天（兼容）"
            : $"{FormatMinute(employee.ShiftStartMinute)}–{FormatMinute(employee.ShiftEndMinute)}";
        return new ShopObjectDetailViewModel(
            BusinessShopInteractionKind.Employee,
            employee.EmployeeId,
            employee.Name,
            role,
            $"效率 {FormatPermille(employee.EffectiveEfficiencyPermille)} · 工资 {FormatMoney(employee.HourlyWageCents)}/小时",
            $"体力 {FormatPermille(employee.EnergyPermille)} · 满意度 {FormatPermille(employee.SatisfactionPermille)} · 班次 {shift} · 任务 {EmployeeTaskTextFormatter.FormatTask(employee.CurrentTask)}",
            employee.EmployeeId,
            $"系统自动排班与分配任务 · {EmployeeTaskTextFormatter.FormatPriorities(employee.TaskPriorities)}",
            IsAutoRestockEnabled: false);
    }

    private void SelectStoreById(string storeId) =>
        SelectStore(Stores.Single(store => store.Id == storeId));

    private void SynchronizeStores(IReadOnlyList<StoreCatalogItemSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            var existing = Stores.SingleOrDefault(store => store.Id == snapshot.Id);
            if (existing is null)
            {
                Stores.Add(new StoreNavigationItemViewModel(snapshot));
            }
            else
            {
                existing.Update(snapshot);
            }
        }
    }

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);

    private static string FormatPermille(int value) =>
        string.Format(CultureInfo.InvariantCulture, "{0:0}%", value / 10m);

    private static string FormatMinute(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static string FormatGameTime(long gameMinute)
    {
        var day = (gameMinute / 1_440) + 1;
        var minuteOfDay = gameMinute % 1_440;
        return $"第 {day} 天 {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
    }
}
