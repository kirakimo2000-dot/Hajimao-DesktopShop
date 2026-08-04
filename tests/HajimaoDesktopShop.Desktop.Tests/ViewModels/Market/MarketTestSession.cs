using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

internal static class MarketTestSession
{
    public static BusinessSession Create(long openingCashCents = 100_000) =>
        BusinessSession.Create(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient", 1)],
            DesktopGameContent.Shops,
            new LevelCurve([0, 40, 120, 300, 650, 1_200]),
            DesktopGameContent.StarterStoreId,
            openingCashCents,
            DesktopGameContent.CreateStarterAssignments(),
            new DeterministicRandomSource(42),
            new BusinessSimulationOptions());
}
