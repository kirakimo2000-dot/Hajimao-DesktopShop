using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Investments;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class StoreOpeningInvestmentAdvisorTests
{
    [Theory]
    [InlineData(2, 100_000, InvestmentAvailability.Available)]
    [InlineData(3, 79_999, InvestmentAvailability.InsufficientFunds)]
    [InlineData(3, 80_000, InvestmentAvailability.Available)]
    public void Create_UsesExactCostAndSharedCashWithoutLevelGateOrPromisedProfit(
        int playerLevel,
        long cashCents,
        InvestmentAvailability expectedAvailability)
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: cashCents);
        var snapshot = session.Simulation.GetSnapshot() with
        {
            Business = session.Simulation.GetSnapshot().Business with
            {
                PlayerLevel = playerLevel,
                CashCents = cashCents
            }
        };
        var economy = StoreEconomyAnalysisService.Calculate(snapshot, "store-1")!;
        var catalog = new[]
        {
            new StoreCatalogItemSnapshot("store-1", "街角店", 1, 0, IsOpen: true),
            new StoreCatalogItemSnapshot("store-2", "车站店", 3, 80_000, IsOpen: false)
        };

        var candidate = Assert.Single(StoreOpeningInvestmentAdvisor.Create(snapshot, catalog, economy));

        Assert.Equal("store:open:store-2", candidate.Id);
        Assert.Equal(InvestmentKind.OpenStore, candidate.Kind);
        Assert.Equal("store-2", candidate.StoreId);
        Assert.Equal("store-2", candidate.TargetId);
        Assert.Equal("车站店", candidate.TargetName);
        Assert.Equal(80_000, candidate.Return.CostCents);
        Assert.Equal(cashCents - 80_000, candidate.Return.CashAfterInvestmentCents);
        Assert.Equal(0, candidate.Return.ExpectedDailyNetBenefitCents);
        Assert.Null(candidate.Return.PaybackDaysTenths);
        Assert.Equal(1, candidate.Effect.StoreCountChange);
        Assert.Equal(0, candidate.RequiredPlayerLevel);
        Assert.Equal(InvestmentEstimateCondition.NewStoreNeedsCompletedDay, candidate.EstimateCondition);
        Assert.Equal(expectedAvailability, candidate.Availability);
    }

    [Fact]
    public void Create_ReturnsEveryUnopenedStoreInCatalogOrder()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        var snapshot = session.Simulation.GetSnapshot();
        var catalog = new[]
        {
            new StoreCatalogItemSnapshot("store-1", "街角店", 1, 0, IsOpen: true),
            new StoreCatalogItemSnapshot("store-3", "社区店", 5, 200_000, IsOpen: false),
            new StoreCatalogItemSnapshot("store-2", "车站店", 3, 80_000, IsOpen: false)
        };

        var candidates = StoreOpeningInvestmentAdvisor.Create(
            snapshot,
            catalog,
            StoreEconomyAnalysisService.Calculate(snapshot, "store-1")!);

        Assert.Equal(["store-3", "store-2"], candidates.Select(candidate => candidate.TargetId));
    }
}
