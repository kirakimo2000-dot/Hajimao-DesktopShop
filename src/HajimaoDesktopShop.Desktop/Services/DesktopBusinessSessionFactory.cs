using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopBusinessSessionFactory
{
    public static BusinessSession Create(
        IReadOnlyList<ProductDefinition> products,
        GameSaveData? save,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(products));
        }

        var random = new DeterministicRandomSource(seed);
        return save is null
            ? BusinessSession.Create(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                openingCashCents: 50_000,
                DesktopGameContent.CreateStarterAssignments(),
                random)
            : BusinessSession.RestoreOrUpgrade(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                save,
                DesktopGameContent.CreateStarterAssignments(),
                random);
    }
}
