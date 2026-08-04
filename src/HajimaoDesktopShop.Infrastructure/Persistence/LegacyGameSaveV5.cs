using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV5(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business,
    BusinessSimulationSaveData? BusinessSimulation)
{
    public GameSaveData UpgradeToV6() =>
        new(
            6,
            SavedAtUtc,
            Shop,
            Simulation,
            Business,
            BusinessSimulation);
}
