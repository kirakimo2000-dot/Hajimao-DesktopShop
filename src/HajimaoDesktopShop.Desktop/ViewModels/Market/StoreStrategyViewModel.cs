using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreStrategyViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private string _currentPricingText = "均衡";
    private string _currentStockingText = "均衡备货";
    private string _statusMessage = "选择整店策略，日常运营由系统完成";
    private string _recoveryGuidanceText = string.Empty;
    private bool _hasRecoveryRecommendation;
    private StoreRecoveryRecommendation? _recoveryRecommendation;
    private readonly RelayCommand _applyRecoveryCommand;

    public StoreStrategyViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _selectedStoreId = selectedStoreId ?? throw new ArgumentNullException(nameof(selectedStoreId));
        UseHighTurnoverPricingCommand = new RelayCommand(() => ApplyPricing(StorePricingPreset.HighTurnover));
        UseBalancedPricingCommand = new RelayCommand(() => ApplyPricing(StorePricingPreset.Balanced));
        UseHighMarginPricingCommand = new RelayCommand(() => ApplyPricing(StorePricingPreset.HighMargin));
        UseLeanStockingCommand = new RelayCommand(() => ApplyStocking(StoreStockingPreset.Lean));
        UseBalancedStockingCommand = new RelayCommand(() => ApplyStocking(StoreStockingPreset.Balanced));
        UseFullShelvesStockingCommand = new RelayCommand(() => ApplyStocking(StoreStockingPreset.FullShelves));
        _applyRecoveryCommand = new RelayCommand(
            ApplyRecovery,
            () => _recoveryRecommendation is not null);
        Refresh();
    }

    public ObservableCollection<StoreStrategyProductViewModel> Products { get; } = [];

    public IRelayCommand UseHighTurnoverPricingCommand { get; }
    public IRelayCommand UseBalancedPricingCommand { get; }
    public IRelayCommand UseHighMarginPricingCommand { get; }
    public IRelayCommand UseLeanStockingCommand { get; }
    public IRelayCommand UseBalancedStockingCommand { get; }
    public IRelayCommand UseFullShelvesStockingCommand { get; }
    public IRelayCommand ApplyRecoveryCommand => _applyRecoveryCommand;

    public string CurrentPricingText
    {
        get => _currentPricingText;
        private set => SetProperty(ref _currentPricingText, value);
    }

    public string CurrentStockingText
    {
        get => _currentStockingText;
        private set => SetProperty(ref _currentStockingText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasRecoveryRecommendation
    {
        get => _hasRecoveryRecommendation;
        private set => SetProperty(ref _hasRecoveryRecommendation, value);
    }

    public string RecoveryGuidanceText
    {
        get => _recoveryGuidanceText;
        private set => SetProperty(ref _recoveryGuidanceText, value);
    }

    public void Refresh()
    {
        var storeId = _selectedStoreId();
        var store = _session.Game.GetSnapshot().Stores.SingleOrDefault(item => item.Id == storeId);
        if (store is null)
        {
            Products.Clear();
            SetRecoveryRecommendation(null);
            return;
        }

        var plan = _session.Strategy.GetAppliedPlan(storeId)
            ?? StoreStrategyPlanner.Create(
                store,
                StorePricingPreset.Balanced,
                StoreStockingPreset.Balanced);
        CurrentPricingText = FormatPricing(plan.Pricing);
        CurrentStockingText = FormatStocking(plan.Stocking);
        Products.Clear();
        foreach (var productPlan in plan.Products)
        {
            var product = store.Products.Single(item => item.Id == productPlan.ProductId);
            Products.Add(new StoreStrategyProductViewModel(
                productPlan.ProductId,
                product.Name,
                FormatMoney(productPlan.SalePriceCents),
                $"库存 ≤ {productPlan.ReorderPoint} 时补至 {productPlan.TargetQuantity}"));
        }

        var analysis = StoreEconomyAnalysisService.Calculate(
            _session.Simulation.GetSnapshot(),
            storeId);
        SetRecoveryRecommendation(analysis is null
            ? null
            : StoreRecoveryAdvisor.Create(analysis));
    }

    private void ApplyPricing(StorePricingPreset pricing)
    {
        var current = _session.Strategy.GetAppliedPlan(_selectedStoreId());
        Apply(pricing, current?.Stocking ?? StoreStockingPreset.Balanced);
    }

    private void ApplyStocking(StoreStockingPreset stocking)
    {
        var current = _session.Strategy.GetAppliedPlan(_selectedStoreId());
        Apply(current?.Pricing ?? StorePricingPreset.Balanced, stocking);
    }

    private void Apply(StorePricingPreset pricing, StoreStockingPreset stocking)
    {
        var result = _session.Strategy.Apply(_selectedStoreId(), pricing, stocking);
        StatusMessage = result.Status == StoreStrategyCommandStatus.Success
            ? "整店策略已应用，采购与补货会自动执行"
            : $"策略应用失败：{result.Status}";
        Refresh();
    }

    private void ApplyRecovery()
    {
        if (_recoveryRecommendation is null)
        {
            return;
        }

        var result = _session.Strategy.ApplyRecovery(_recoveryRecommendation);
        StatusMessage = result.Status == StoreStrategyCommandStatus.Success
            ? "保守方案已应用：降低库存占用并优先修复现金流"
            : $"保守方案应用失败：{result.Status}";
        Refresh();
    }

    private void SetRecoveryRecommendation(StoreRecoveryRecommendation? recommendation)
    {
        _recoveryRecommendation = recommendation;
        HasRecoveryRecommendation = recommendation is not null;
        RecoveryGuidanceText = recommendation?.EvidenceCode switch
        {
            "negative-profit" => "最近经营出现亏损；可采用精益库存并按瓶颈调整定价。",
            "critical-cash-runway" => "现金跑道不足一个经营日；可先降低库存占用。",
            _ => string.Empty
        };
        _applyRecoveryCommand.NotifyCanExecuteChanged();
    }

    private static string FormatPricing(StorePricingPreset preset) => preset switch
    {
        StorePricingPreset.HighTurnover => "高周转",
        StorePricingPreset.Balanced => "均衡",
        StorePricingPreset.HighMargin => "高毛利",
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    private static string FormatStocking(StoreStockingPreset preset) => preset switch
    {
        StoreStockingPreset.Lean => "精益库存",
        StoreStockingPreset.Balanced => "均衡备货",
        StoreStockingPreset.FullShelves => "充足货架",
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}

public sealed record StoreStrategyProductViewModel(
    string ProductId,
    string Name,
    string PriceText,
    string StockingText);
