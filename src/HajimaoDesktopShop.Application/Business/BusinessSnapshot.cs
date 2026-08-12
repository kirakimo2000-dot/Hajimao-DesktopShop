using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Domain.Demand;

namespace HajimaoDesktopShop.Application.Business;

public sealed record BusinessSnapshot(
    int PlayerLevel,
    long TotalExperience,
    long CashCents,
    IReadOnlyList<BusinessStoreSnapshot> Stores);

public sealed record BusinessStoreSnapshot(
    string Id,
    string Name,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    IReadOnlyList<ProductSnapshot> Products,
    long WageCostCents = 0,
    long NetProfitCents = 0,
    long OperatingCostCents = 0,
    StoreGrowthSnapshot? Growth = null,
    string StoreBrandId = "legacy",
    string StoreFormatId = "legacy",
    int StreetOrdinal = 1,
    StoreFormatEconomicsSnapshot? FormatEconomics = null);

public sealed record StoreFormatEconomicsSnapshot(
    DemandSensitivity DemandSensitivity,
    DemandTimeCurve TimeCurve,
    int InventoryCapacityPermille,
    IReadOnlyDictionary<string, int> ProductShelfWeights)
{
    public static StoreFormatEconomicsSnapshot Neutral { get; } = new(
        DemandSensitivity.Neutral,
        DemandTimeCurve.Steady,
        1_000,
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["ambient"] = 1_000,
            ["chilled"] = 1_000,
            ["frozen"] = 1_000
        });
}
