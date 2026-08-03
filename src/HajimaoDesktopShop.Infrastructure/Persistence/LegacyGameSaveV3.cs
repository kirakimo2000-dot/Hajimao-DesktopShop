using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV3(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business,
    BusinessSimulationSaveData? BusinessSimulation)
{
    public GameSaveData UpgradeToV4()
    {
        var business = Business is null
            ? null
            : Business with
            {
                Procurement = Business.Procurement ?? new BusinessProcurementSaveData(1, [], [])
            };
        return new GameSaveData(
            GameSaveSchema.CurrentVersion,
            SavedAtUtc,
            Shop,
            Simulation,
            business,
            BusinessSimulation);
    }
}
