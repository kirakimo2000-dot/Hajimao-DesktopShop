using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreGrowthManagementViewModel : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, string> PromotionNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["local-flyers"] = "本地传单",
            ["discount-coupons"] = "折扣券",
            ["festival-event"] = "节庆活动"
        };

    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private int _expansionLevel;
    private int _shelfLevel;
    private int _decorationLevel;
    private string _inventoryCapacityText = "100%";
    private string _activePromotionText = "暂无促销";
    private string _statusMessage = "店铺成长已就绪";

    public StoreGrowthManagementViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedStoreId);
        _session = session;
        _selectedStoreId = selectedStoreId;
        UpgradeExpansionCommand = new RelayCommand(() => Upgrade(StoreUpgradeKind.Expansion));
        UpgradeShelfCommand = new RelayCommand(() => Upgrade(StoreUpgradeKind.Shelf));
        UpgradeDecorationCommand = new RelayCommand(() => Upgrade(StoreUpgradeKind.Decoration));
        StartFlyersCommand = new RelayCommand(() => StartPromotion("local-flyers"));
        StartCouponsCommand = new RelayCommand(() => StartPromotion("discount-coupons"));
        StartFestivalCommand = new RelayCommand(() => StartPromotion("festival-event"));
        Refresh();
    }

    public event EventHandler<GameFeedbackEventArgs>? FeedbackRaised;

    public IRelayCommand UpgradeExpansionCommand { get; }
    public IRelayCommand UpgradeShelfCommand { get; }
    public IRelayCommand UpgradeDecorationCommand { get; }
    public IRelayCommand StartFlyersCommand { get; }
    public IRelayCommand StartCouponsCommand { get; }
    public IRelayCommand StartFestivalCommand { get; }

    public int ExpansionLevel
    {
        get => _expansionLevel;
        private set => SetProperty(ref _expansionLevel, value);
    }

    public int ShelfLevel
    {
        get => _shelfLevel;
        private set => SetProperty(ref _shelfLevel, value);
    }

    public int DecorationLevel
    {
        get => _decorationLevel;
        private set => SetProperty(ref _decorationLevel, value);
    }

    public string InventoryCapacityText
    {
        get => _inventoryCapacityText;
        private set => SetProperty(ref _inventoryCapacityText, value);
    }

    public string ActivePromotionText
    {
        get => _activePromotionText;
        private set => SetProperty(ref _activePromotionText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        var storeId = _selectedStoreId();
        if (!_session.Game.IsStoreOpen(storeId))
        {
            return;
        }

        var snapshot = _session.Game.GetStoreGrowthSnapshot(storeId);
        ExpansionLevel = snapshot.ExpansionLevel;
        ShelfLevel = snapshot.ShelfLevel;
        DecorationLevel = snapshot.DecorationLevel;
        InventoryCapacityText = string.Format(
            CultureInfo.InvariantCulture,
            "{0:0}%",
            snapshot.InventoryCapacityPermille / 10m);
        ActivePromotionText = snapshot.ActivePromotion is null
            ? "暂无促销"
            : $"{PromotionNames[snapshot.ActivePromotion.CampaignId]} · 剩余 {snapshot.ActivePromotion.RemainingMinutes} 分钟";
    }

    private void Upgrade(StoreUpgradeKind kind)
    {
        var result = _session.Game.UpgradeStore(_selectedStoreId(), kind);
        CompleteCommand("升级", result, GameFeedbackKind.StoreGrowthChanged);
    }

    private void StartPromotion(string campaignId)
    {
        var result = _session.Game.StartPromotion(_selectedStoreId(), campaignId);
        CompleteCommand("促销", result, GameFeedbackKind.PromotionStarted);
    }

    private void CompleteCommand(
        string operation,
        StoreGrowthCommandResult result,
        GameFeedbackKind feedbackKind)
    {
        StatusMessage = result.Status == StoreGrowthCommandStatus.Success
            ? $"{operation}成功"
            : $"{operation}失败：{result.Status}";
        if (result.Status == StoreGrowthCommandStatus.Success)
        {
            FeedbackRaised?.Invoke(this, new GameFeedbackEventArgs(feedbackKind));
        }

        Refresh();
    }
}
