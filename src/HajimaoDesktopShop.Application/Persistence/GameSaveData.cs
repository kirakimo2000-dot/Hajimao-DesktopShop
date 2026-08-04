using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Customers;

namespace HajimaoDesktopShop.Application.Persistence;

public static class GameSaveSchema
{
    public const int CurrentVersion = 5;
}

public sealed record GameSaveData(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business = null,
    BusinessSimulationSaveData? BusinessSimulation = null);

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
