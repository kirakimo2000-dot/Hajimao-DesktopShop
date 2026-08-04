using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private ManagementSection _selectedSection = ManagementSection.Store;
    private string _selectedStoreId = string.Empty;
    private string _selectedStoreName = string.Empty;
    private string _cashText = "¥0.00";
    private string _playerLevelText = "Lv.1";
    private string _gameTimeText = "第 1 天 00:00";
    private string _stockWarningText = "缺货/低库存 0";
    private string _customerCountText = "顾客/队列 0";
    private BusinessShopFrame? _sceneFrame;

    public MarketViewModel(BusinessSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        NavigateCommand = new RelayCommand<ManagementSection>(Navigate);
        SelectStoreCommand = new RelayCommand<StoreNavigationItemViewModel>(SelectStore);
        Overview = new MarketOverviewViewModel(session.Game, Refresh);
        ProductManagement = new ProductManagementViewModel(session, () => SelectedStoreId);
        EmployeeManagement = new EmployeeManagementViewModel(session, () => SelectedStoreId);
        StoreGrowth = new StoreGrowthManagementViewModel(session, () => SelectedStoreId);
        Finance = new FinanceViewModel(session, () => SelectedStoreId);
        Refresh();
    }

    public ObservableCollection<StoreNavigationItemViewModel> Stores { get; } = [];

    public MarketOverviewViewModel Overview { get; }

    public ProductManagementViewModel ProductManagement { get; }

    public EmployeeManagementViewModel EmployeeManagement { get; }

    public StoreGrowthManagementViewModel StoreGrowth { get; }

    public FinanceViewModel Finance { get; }

    public IRelayCommand<ManagementSection> NavigateCommand { get; }

    public IRelayCommand<StoreNavigationItemViewModel> SelectStoreCommand { get; }

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

            OnPropertyChanged(nameof(IsStoreSection));
            OnPropertyChanged(nameof(IsProductsSection));
            OnPropertyChanged(nameof(IsProcurementSection));
            OnPropertyChanged(nameof(IsEmployeesSection));
            OnPropertyChanged(nameof(IsGrowthSection));
            OnPropertyChanged(nameof(IsFinanceSection));
            OnPropertyChanged(nameof(IsCommercialStreetSection));
        }
    }

    public bool IsStoreSection => SelectedSection == ManagementSection.Store;
    public bool IsProductsSection => SelectedSection == ManagementSection.Products;
    public bool IsProcurementSection => SelectedSection == ManagementSection.Procurement;
    public bool IsEmployeesSection => SelectedSection == ManagementSection.Employees;
    public bool IsGrowthSection => SelectedSection == ManagementSection.Growth;
    public bool IsFinanceSection => SelectedSection == ManagementSection.Finance;
    public bool IsCommercialStreetSection => SelectedSection == ManagementSection.CommercialStreet;

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

    public BusinessShopFrame? SceneFrame
    {
        get => _sceneFrame;
        private set => SetProperty(ref _sceneFrame, value);
    }

    public void Refresh()
    {
        var snapshot = _session.Simulation.GetSnapshot();
        SynchronizeStores(_session.Game.GetStoreCatalogSnapshot());

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

        CashText = FormatMoney(snapshot.Business.CashCents);
        PlayerLevelText = $"Lv.{snapshot.Business.PlayerLevel}";
        GameTimeText = FormatGameTime(snapshot.GameMinute);

        var store = snapshot.Business.Stores.SingleOrDefault(item => item.Id == SelectedStoreId);
        var operations = snapshot.Stores.SingleOrDefault(item => item.StoreId == SelectedStoreId);
        var warningCount = store?.Products.Count(product =>
            product.Quantity == 0 || product.Quantity * 4 < product.Capacity) ?? 0;
        StockWarningText = $"缺货/低库存 {warningCount}";
        CustomerCountText = $"顾客/队列 {operations?.CheckoutQueueLength ?? 0}";
        SceneFrame = new BusinessShopFrame(
            snapshot,
            SelectedStoreId,
            CashText,
            PlayerLevelText,
            GameTimeText,
            StockWarningText,
            CustomerCountText,
            IsLocked: false,
            IsClickThrough: false);
        Overview.Synchronize(Stores);
        ProductManagement.Refresh();
        EmployeeManagement.Refresh();
        StoreGrowth.Refresh();
        Finance.Refresh();
    }

    private void Navigate(ManagementSection section) => SelectedSection = section;

    private void SelectStore(StoreNavigationItemViewModel? store)
    {
        if (store is null)
        {
            return;
        }

        SelectedStoreId = store.Id;
        SelectedStoreName = store.Name;
        Refresh();
    }

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

    private static string FormatGameTime(long gameMinute)
    {
        var day = (gameMinute / 1_440) + 1;
        var minuteOfDay = gameMinute % 1_440;
        return $"第 {day} 天 {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
    }
}
