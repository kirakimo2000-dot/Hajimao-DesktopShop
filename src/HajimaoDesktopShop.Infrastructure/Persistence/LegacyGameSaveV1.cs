using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV1(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    LegacySimulationSaveDataV1 Simulation)
{
    public GameSaveData Upgrade() =>
        new(
            GameSaveSchema.CurrentVersion,
            SavedAtUtc,
            Shop,
            new SimulationSaveData(
                Simulation.GameMinute,
                Simulation.Tick,
                Simulation.NextCustomerId,
                Simulation.CompletedSales,
                Simulation.Customers,
                Simulation.CheckoutQueue,
                Simulation.CashierCustomerId,
                Simulation.RestockQueue,
                Simulation.ActiveRestockTask,
                Simulation.LastRestockFailure));
}

internal sealed record LegacySimulationSaveDataV1(
    long GameMinute,
    int Speed,
    long Tick,
    long NextCustomerId,
    int CompletedSales,
    IReadOnlyList<CustomerSaveData> Customers,
    IReadOnlyList<long> CheckoutQueue,
    long? CashierCustomerId,
    IReadOnlyList<RestockTaskSaveData> RestockQueue,
    RestockTaskSaveData? ActiveRestockTask,
    string? LastRestockFailure);
