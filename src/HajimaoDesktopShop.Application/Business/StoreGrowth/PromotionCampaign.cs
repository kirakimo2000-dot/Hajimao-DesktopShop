namespace HajimaoDesktopShop.Application.Business.StoreGrowth;

public sealed record PromotionCampaign(
    string Id,
    long CostCents,
    int DurationMinutes,
    int ArrivalBonusBasisPoints,
    int PurchaseBonusBasisPoints,
    int RequiredExpansionLevel,
    int RequiredDecorationLevel);

public sealed record ActivePromotionSnapshot(
    string CampaignId,
    long CostCents,
    int RemainingMinutes,
    int ArrivalBonusBasisPoints,
    int PurchaseBonusBasisPoints);

internal sealed record StorePromotionState(
    string StoreId,
    string CampaignId,
    int RemainingMinutes);
