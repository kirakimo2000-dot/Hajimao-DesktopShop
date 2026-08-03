using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Desktop.ViewModels;

public sealed class GameViewModel : ObservableObject
{
    private readonly ShopGameService _game;
    private readonly ShopSimulation _simulation;
    private readonly Dictionary<string, ProductItemViewModel> _productsById =
        new(StringComparer.Ordinal);
    private string _cashText = "¥0.00";
    private string _gameTimeText = "第 1 天 00:00";
    private string _customerCountText = "顾客 0";
    private string _stockWarningText = "缺货/低库存 0";
    private string _revenueText = "¥0.00";
    private string _expenseText = "¥0.00";
    private string _grossProfitText = "¥0.00";
    private string _statusMessage = "小店准备营业";
    private bool _isLocked;
    private bool _isClickThrough;
    private SimulationSnapshot _sceneSnapshot;
    private int _lastCompletedSales;
    private bool _isMuted;
    private DesktopShopFrame _desktopFrame;

    public GameViewModel(ShopGameService game, ShopSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(simulation);
        _game = game;
        _simulation = simulation;
        _sceneSnapshot = simulation.GetSnapshot();
        _lastCompletedSales = _sceneSnapshot.CompletedSales;
        _desktopFrame = CreateDesktopFrame();

        QueueRestockCommand = new RelayCommand<ProductItemViewModel>(QueueRestock);
        IncreasePriceCommand = new RelayCommand<ProductItemViewModel>(IncreasePrice);
        DecreasePriceCommand = new RelayCommand<ProductItemViewModel>(DecreasePrice);
        ToggleLockCommand = new RelayCommand(ToggleLock);
        ToggleClickThroughCommand = new RelayCommand(ToggleClickThrough);
        ToggleMuteCommand = new RelayCommand(ToggleMute);
        Refresh();
    }

    public event EventHandler<GameFeedbackEventArgs>? FeedbackRaised;

    public ObservableCollection<ProductItemViewModel> Products { get; } = [];

    public ObservableCollection<CustomerVisualViewModel> Customers { get; } = [];

    public ObservableCollection<EmployeeItemViewModel> Employees { get; } = [];

    public IRelayCommand<ProductItemViewModel> QueueRestockCommand { get; }

    public IRelayCommand<ProductItemViewModel> IncreasePriceCommand { get; }

    public IRelayCommand<ProductItemViewModel> DecreasePriceCommand { get; }

    public IRelayCommand ToggleLockCommand { get; }

    public IRelayCommand ToggleClickThroughCommand { get; }

    public IRelayCommand ToggleMuteCommand { get; }

    public string CashText
    {
        get => _cashText;
        private set => SetProperty(ref _cashText, value);
    }

    public string GameTimeText
    {
        get => _gameTimeText;
        private set => SetProperty(ref _gameTimeText, value);
    }

    public string CustomerCountText
    {
        get => _customerCountText;
        private set => SetProperty(ref _customerCountText, value);
    }

    public string StockWarningText
    {
        get => _stockWarningText;
        private set => SetProperty(ref _stockWarningText, value);
    }

    public string RevenueText
    {
        get => _revenueText;
        private set => SetProperty(ref _revenueText, value);
    }

    public string ExpenseText
    {
        get => _expenseText;
        private set => SetProperty(ref _expenseText, value);
    }

    public string GrossProfitText
    {
        get => _grossProfitText;
        private set => SetProperty(ref _grossProfitText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        private set
        {
            if (SetProperty(ref _isLocked, value))
            {
                UpdateDesktopFrame();
            }
        }
    }

    public bool IsClickThrough
    {
        get => _isClickThrough;
        private set
        {
            if (SetProperty(ref _isClickThrough, value))
            {
                UpdateDesktopFrame();
            }
        }
    }

    public SimulationSnapshot SceneSnapshot
    {
        get => _sceneSnapshot;
        private set => SetProperty(ref _sceneSnapshot, value);
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

    public DesktopShopFrame DesktopFrame
    {
        get => _desktopFrame;
        private set => SetProperty(ref _desktopFrame, value);
    }

    public void Refresh()
    {
        var snapshot = _simulation.GetSnapshot();
        SceneSnapshot = snapshot;
        if (snapshot.CompletedSales > _lastCompletedSales)
        {
            _lastCompletedSales = snapshot.CompletedSales;
            RaiseFeedback(GameFeedbackKind.SaleCompleted);
        }
        CashText = FormatMoney(snapshot.Shop.CashCents);
        RevenueText = FormatMoney(snapshot.Shop.RevenueCents);
        ExpenseText = FormatMoney(snapshot.Shop.StockPurchaseCostCents);
        GrossProfitText = FormatMoney(snapshot.Shop.GrossProfitCents);
        GameTimeText = FormatGameTime(snapshot.GameMinute);
        CustomerCountText = $"顾客 {snapshot.Customers.Count}";

        UpdateProducts(snapshot);
        UpdateCustomers(snapshot);
        UpdateEmployees(snapshot);

        var warningCount = snapshot.Shop.Products.Count(product =>
            product.Quantity == 0 || product.Quantity * 4 < product.Capacity);
        StockWarningText = $"缺货/低库存 {warningCount}";

        if (snapshot.LastRestockFailure is { } failure)
        {
            StatusMessage = $"补货失败：{failure}。请检查资金、容量或商品。";
        }

        UpdateDesktopFrame();
    }

    public void RestoreDesktopState(bool isLocked)
    {
        IsLocked = isLocked;
        IsClickThrough = false;
    }

    public void ReportSystemMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            StatusMessage = message.Trim();
        }
    }

    private void QueueRestock(ProductItemViewModel? product)
    {
        if (product is null)
        {
            return;
        }

        _simulation.QueueRestock(product.Id, 5);
        StatusMessage = $"{product.Name} 已排入补货 ×5";
        RaiseFeedback(GameFeedbackKind.RestockQueued);
    }

    private void IncreasePrice(ProductItemViewModel? product) => ChangePrice(product, 10);

    private void DecreasePrice(ProductItemViewModel? product) => ChangePrice(product, -10);

    private void ChangePrice(ProductItemViewModel? product, long deltaCents)
    {
        if (product is null)
        {
            return;
        }

        var result = _game.ChangePrice(product.Id, product.SalePriceCents + deltaCents);
        StatusMessage = result.Status == PriceChangeStatus.Success
            ? $"{product.Name} 售价已调整为 {FormatMoney(result.SalePrice.Cents)}"
            : $"调价失败：{result.Status}";
        if (result.Status == PriceChangeStatus.Success)
        {
            RaiseFeedback(GameFeedbackKind.PriceChanged);
        }
    }

    private void ToggleLock()
    {
        IsLocked = !IsLocked;
        StatusMessage = IsLocked ? "桌面小店已锁定" : "桌面小店可拖动";
    }

    private void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;
        StatusMessage = IsClickThrough ? "鼠标穿透已开启" : "鼠标穿透已关闭";
    }

    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        StatusMessage = IsMuted ? "音效已静音" : "音效已开启";
    }

    private void RaiseFeedback(GameFeedbackKind kind) =>
        FeedbackRaised?.Invoke(this, new GameFeedbackEventArgs(kind));

    private void UpdateDesktopFrame() => DesktopFrame = CreateDesktopFrame();

    private DesktopShopFrame CreateDesktopFrame() =>
        new(
            SceneSnapshot,
            CashText,
            GameTimeText,
            StockWarningText,
            CustomerCountText,
            IsLocked,
            IsClickThrough);

    private void UpdateProducts(SimulationSnapshot snapshot)
    {
        foreach (var product in snapshot.Shop.Products)
        {
            if (!_productsById.TryGetValue(product.Id, out var item))
            {
                item = new ProductItemViewModel(product);
                _productsById.Add(product.Id, item);
                Products.Add(item);
            }
            else
            {
                item.Update(product);
            }
        }
    }

    private void UpdateCustomers(SimulationSnapshot snapshot)
    {
        Customers.Clear();
        foreach (var customer in snapshot.Customers)
        {
            Customers.Add(CustomerVisualViewModel.FromSnapshot(customer));
        }
    }

    private void UpdateEmployees(SimulationSnapshot snapshot)
    {
        Employees.Clear();
        foreach (var employee in snapshot.Employees)
        {
            Employees.Add(EmployeeItemViewModel.FromSnapshot(employee));
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
