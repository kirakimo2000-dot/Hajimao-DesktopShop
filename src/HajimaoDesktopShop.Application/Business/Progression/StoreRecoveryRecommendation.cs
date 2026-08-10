using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Business.Progression;

public sealed record StoreRecoveryRecommendation(
    string StoreId,
    StoreBottleneck Bottleneck,
    StorePricingPreset Pricing,
    StoreStockingPreset Stocking,
    string EvidenceCode);
