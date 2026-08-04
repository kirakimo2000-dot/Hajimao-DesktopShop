using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.StoreGrowth;

internal sealed class StoreGrowthService
{
    private static readonly IReadOnlyDictionary<string, PromotionCampaign> Campaigns =
        new Dictionary<string, PromotionCampaign>(StringComparer.Ordinal)
        {
            ["local-flyers"] = new(
                "local-flyers", 15_000, 240, 1_200, 0, 0, 0),
            ["discount-coupons"] = new(
                "discount-coupons", 25_000, 360, 600, 800, 0, 1),
            ["festival-event"] = new(
                "festival-event", 50_000, 480, 1_600, 600, 2, 2)
        };

    private readonly IStoreGrowthGateway _gateway;
    private readonly Dictionary<string, ActivePromotion> _activePromotions =
        new(StringComparer.Ordinal);

    public StoreGrowthService(
        IStoreGrowthGateway gateway,
        IEnumerable<StorePromotionState>? restoredPromotions = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
        RestorePromotions(restoredPromotions ?? []);
    }

    public StoreGrowthCommandResult UpgradeStore(string storeId, StoreUpgradeKind kind)
    {
        var normalizedId = NormalizeStoreId(storeId);
        var result = _gateway.TryUpgradeStore(normalizedId, kind);
        return new StoreGrowthCommandResult(Map(result.Status), result.Cost.Cents);
    }

    public StoreGrowthCommandResult StartPromotion(string storeId, string campaignId)
    {
        var normalizedStoreId = NormalizeStoreId(storeId);
        if (string.IsNullOrWhiteSpace(campaignId))
        {
            throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        }

        var development = _gateway.FindDevelopment(normalizedStoreId);
        if (development is null)
        {
            return new StoreGrowthCommandResult(StoreGrowthCommandStatus.UnknownStore, 0);
        }

        if (!Campaigns.TryGetValue(campaignId.Trim(), out var campaign))
        {
            return new StoreGrowthCommandResult(StoreGrowthCommandStatus.UnknownPromotion, 0);
        }

        if (_activePromotions.ContainsKey(normalizedStoreId))
        {
            return new StoreGrowthCommandResult(
                StoreGrowthCommandStatus.PromotionAlreadyActive,
                campaign.CostCents);
        }

        if (development.ExpansionLevel < campaign.RequiredExpansionLevel ||
            development.DecorationLevel < campaign.RequiredDecorationLevel)
        {
            return new StoreGrowthCommandResult(
                StoreGrowthCommandStatus.PrerequisiteNotMet,
                campaign.CostCents);
        }

        if (!_gateway.TryChargePromotion(normalizedStoreId, new Money(campaign.CostCents)))
        {
            return new StoreGrowthCommandResult(
                StoreGrowthCommandStatus.InsufficientFunds,
                campaign.CostCents);
        }

        _activePromotions.Add(
            normalizedStoreId,
            new ActivePromotion(campaign, campaign.DurationMinutes));
        return new StoreGrowthCommandResult(StoreGrowthCommandStatus.Success, campaign.CostCents);
    }

    public void AdvanceMinute() => AdvanceMinutes(1);

    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        if (minutes == 0 || _activePromotions.Count == 0)
        {
            return;
        }

        foreach (var storeId in _activePromotions.Keys.ToArray())
        {
            var active = _activePromotions[storeId];
            var remaining = active.RemainingMinutes - minutes;
            if (remaining <= 0)
            {
                _activePromotions.Remove(storeId);
            }
            else
            {
                _activePromotions[storeId] = active with { RemainingMinutes = remaining };
            }
        }
    }

    public StoreGrowthSnapshot GetSnapshot(string storeId)
    {
        var normalizedStoreId = NormalizeStoreId(storeId);
        var development = _gateway.FindDevelopment(normalizedStoreId)
            ?? throw new KeyNotFoundException($"Store '{normalizedStoreId}' was not found.");
        _activePromotions.TryGetValue(normalizedStoreId, out var active);
        var promotion = active is null
            ? null
            : new ActivePromotionSnapshot(
                active.Campaign.Id,
                active.Campaign.CostCents,
                active.RemainingMinutes,
                active.Campaign.ArrivalBonusBasisPoints,
                active.Campaign.PurchaseBonusBasisPoints);

        return new StoreGrowthSnapshot(
            normalizedStoreId,
            development.ExpansionLevel,
            development.ShelfLevel,
            development.DecorationLevel,
            development.FloorAreaUnits,
            development.ShelfSlotCount,
            development.QueueComfortCapacity,
            development.InventoryCapacityPermille,
            development.AttractionBonusBasisPoints,
            NextCost(development, StoreUpgradeKind.Expansion),
            NextCost(development, StoreUpgradeKind.Shelf),
            NextCost(development, StoreUpgradeKind.Decoration),
            promotion?.ArrivalBonusBasisPoints ?? 0,
            promotion?.PurchaseBonusBasisPoints ?? 0,
            promotion);
    }

    public IReadOnlyList<StorePromotionState> CaptureState() =>
        Array.AsReadOnly(_activePromotions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new StorePromotionState(
                pair.Key,
                pair.Value.Campaign.Id,
                pair.Value.RemainingMinutes))
            .ToArray());

    private void RestorePromotions(IEnumerable<StorePromotionState> restoredPromotions)
    {
        ArgumentNullException.ThrowIfNull(restoredPromotions);
        foreach (var state in restoredPromotions)
        {
            ArgumentNullException.ThrowIfNull(state);
            var storeId = NormalizeStoreId(state.StoreId);
            if (_gateway.FindDevelopment(storeId) is null)
            {
                throw new ArgumentException(
                    $"Restored promotion references unknown store '{storeId}'.",
                    nameof(restoredPromotions));
            }

            if (!Campaigns.TryGetValue(state.CampaignId, out var campaign))
            {
                throw new ArgumentException(
                    $"Restored promotion '{state.CampaignId}' is unknown.",
                    nameof(restoredPromotions));
            }

            if (state.RemainingMinutes is <= 0 || state.RemainingMinutes > campaign.DurationMinutes)
            {
                throw new ArgumentOutOfRangeException(nameof(restoredPromotions));
            }

            if (!_activePromotions.TryAdd(
                    storeId,
                    new ActivePromotion(campaign, state.RemainingMinutes)))
            {
                throw new ArgumentException(
                    $"Restored store '{storeId}' has multiple active promotions.",
                    nameof(restoredPromotions));
            }
        }
    }

    private static long? NextCost(StoreDevelopment development, StoreUpgradeKind kind)
    {
        var preview = development.PreviewUpgrade(kind);
        return preview.Status == StoreUpgradeStatus.MaximumLevel ? null : preview.Cost.Cents;
    }

    private static StoreGrowthCommandStatus Map(StoreUpgradeStatus status) => status switch
    {
        StoreUpgradeStatus.Success => StoreGrowthCommandStatus.Success,
        StoreUpgradeStatus.UnknownStore => StoreGrowthCommandStatus.UnknownStore,
        StoreUpgradeStatus.MaximumLevel => StoreGrowthCommandStatus.MaximumLevel,
        StoreUpgradeStatus.PrerequisiteNotMet => StoreGrowthCommandStatus.PrerequisiteNotMet,
        StoreUpgradeStatus.InsufficientFunds => StoreGrowthCommandStatus.InsufficientFunds,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string NormalizeStoreId(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        return storeId.Trim();
    }

    private sealed record ActivePromotion(
        PromotionCampaign Campaign,
        int RemainingMinutes);
}
