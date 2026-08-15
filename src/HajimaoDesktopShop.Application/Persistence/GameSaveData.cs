using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Business.Investments;

namespace HajimaoDesktopShop.Application.Persistence;

public static class GameSaveSchema
{
    public const int CurrentVersion = 8;
}

public sealed record GameSaveData(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business = null,
    BusinessSimulationSaveData? BusinessSimulation = null,
    InvestmentTrackingSaveData? InvestmentTracking = null,
    CombatSaveData? Combat = null);

public sealed record InvestmentTrackingSaveData(
    IReadOnlyList<LatestInvestmentSaveData> LatestInvestments);

public sealed record LatestInvestmentSaveData(
    string StoreId,
    string CandidateId,
    InvestmentKind Kind,
    long CostCents,
    long ExpectedDailyNetBenefitCents,
    long GameMinute,
    int? BaselineDayNumber,
    long? BaselineNetProfitCents,
    int? BaselineCompletedSales,
    int? BaselineLostSales);

public sealed record ShopSaveData(
    long CashCents,
    long TotalRevenueCents,
    long TotalStockPurchaseCostCents,
    long TotalGrossProfitCents,
    IReadOnlyList<ProductSaveData> Products);

public sealed record ProductSaveData(
    string ProductId,
    long SalePriceCents,
    int Quantity);

public sealed record SimulationSaveData(
    long GameMinute,
    long Tick,
    long NextCustomerId,
    int CompletedSales,
    IReadOnlyList<CustomerSaveData> Customers,
    IReadOnlyList<long> CheckoutQueue,
    long? CashierCustomerId,
    IReadOnlyList<RestockTaskSaveData> RestockQueue,
    RestockTaskSaveData? ActiveRestockTask,
    string? LastRestockFailure);

public sealed record CustomerSaveData(
    long Id,
    CustomerState State,
    string? SelectedProductId,
    long LastTransitionTick);

public sealed record RestockTaskSaveData(string ProductId, int Quantity);
