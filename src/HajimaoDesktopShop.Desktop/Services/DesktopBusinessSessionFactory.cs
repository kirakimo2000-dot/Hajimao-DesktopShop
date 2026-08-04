using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopBusinessSessionFactory
{
    public static DesktopBusinessSessionStartResult Create(
        IReadOnlyList<ProductDefinition> products,
        GameSaveData? save,
        int seed,
        DateTimeOffset nowUtc,
        OfflineSettlementPolicy? offlinePolicy = null)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(products));
        }

        var random = new DeterministicRandomSource(seed);
        if (save is null)
        {
            var newSession = BusinessSession.Create(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                openingCashCents: 50_000,
                DesktopGameContent.CreateStarterAssignments(),
                random);
            return new DesktopBusinessSessionStartResult(
                newSession,
                IsNewGame: true,
                OfflineSettlement: null);
        }

        var restoredSession = BusinessSession.RestoreOrUpgrade(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                save,
                DesktopGameContent.CreateStarterAssignments(),
                random);
        var settlement = OfflineSettlementService.Settle(
            restoredSession.Simulation,
            save.SavedAtUtc,
            nowUtc,
            offlinePolicy);
        return new DesktopBusinessSessionStartResult(
            restoredSession,
            IsNewGame: false,
            settlement);
    }
}
