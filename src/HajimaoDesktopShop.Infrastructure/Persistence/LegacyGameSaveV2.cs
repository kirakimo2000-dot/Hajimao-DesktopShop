using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV2(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation)
{
    public GameSaveData UpgradeToV3() =>
        new(
            GameSaveSchema.CurrentVersion,
            SavedAtUtc,
            Shop,
            Simulation);
}
