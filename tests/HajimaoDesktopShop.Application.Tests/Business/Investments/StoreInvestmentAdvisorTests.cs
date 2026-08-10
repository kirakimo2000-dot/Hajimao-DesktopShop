using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Tests.Business;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class StoreInvestmentAdvisorTests
{
    [Fact]
    public void Create_UnifiesGrowthAndCurrentEmployeeCandidatesWithExactCosts()
    {
        var session = BusinessTestSessionFactory.Create();
        var snapshot = session.Simulation.GetSnapshot();
        var growth = session.Game.GetStoreGrowthSnapshot("store-1");
        var economy = StoreEconomyAnalysisService.Calculate(snapshot, "store-1")!;

        var portfolio = StoreInvestmentAdvisor.Create(snapshot, growth, economy);

        Assert.Equal("store-1", portfolio.StoreId);
        Assert.Collection(
            portfolio.Candidates.Take(3),
            candidate =>
            {
                Assert.Equal("growth:expansion", candidate.Id);
                Assert.Equal(60_000, candidate.Return.CostCents);
                Assert.Equal(2, candidate.Effect.ShelfSlotChange);
                Assert.Equal(2, candidate.Effect.QueueComfortChange);
                Assert.Equal(150, candidate.Effect.AttractionChangeBasisPoints);
            },
            candidate =>
            {
                Assert.Equal("growth:shelf", candidate.Id);
                Assert.Equal(25_000, candidate.Return.CostCents);
                Assert.Equal(250, candidate.Effect.InventoryCapacityChangePermille);
            },
            candidate =>
            {
                Assert.Equal("growth:decoration", candidate.Id);
                Assert.Equal(30_000, candidate.Return.CostCents);
                Assert.Equal(250, candidate.Effect.AttractionChangeBasisPoints);
            });
        Assert.Equal(
            snapshot.Employees.Candidates.Select(candidate => $"employee:{candidate.CandidateId}"),
            portfolio.Candidates.Skip(3).Select(candidate => candidate.Id));
        Assert.All(
            portfolio.Candidates.Skip(3),
            candidate => Assert.Equal(
                snapshot.Employees.Candidates.Single(item => item.CandidateId == candidate.TargetId).HireCost.Cents,
                candidate.Return.CostCents));
    }

    [Fact]
    public void Create_UsesObservedStockLossesForConservativeShelfPayback()
    {
        var session = BusinessTestSessionFactory.Create();
        var snapshot = session.Simulation.GetSnapshot();
        var economy = StoreEconomyAnalysisService.Calculate(new StoreEconomyAnalysisInput(
            "store-1",
            SharedCashCents: 100_000,
            RevenueCents: 80_000,
            GrossProfitCents: 40_000,
            WageCostCents: 10_000,
            OperatingCostCents: 5_000,
            NetProfitCents: 25_000,
            Visitors: 40,
            CompletedSales: 20,
            LostSales: 10,
            OutOfStockProductCount: 1,
            CheckoutQueueLength: 0,
            ServicePermille: 900,
            IsCompletedDay: true));

        var shelf = StoreInvestmentAdvisor.Create(
                snapshot,
                session.Game.GetStoreGrowthSnapshot("store-1"),
                economy)
            .Candidates.Single(candidate => candidate.Id == "growth:shelf");

        Assert.Equal(StoreBottleneck.Stock, shelf.AddressedBottleneck);
        Assert.Equal(4_000, shelf.Return.ExpectedDailyNetBenefitCents);
        Assert.Equal(63, shelf.Return.PaybackDaysTenths);
        Assert.Equal(InvestmentEstimateCondition.StockLossesRepeat, shelf.EstimateCondition);
    }

    [Fact]
    public void Create_KeepsPrerequisiteBlockedUpgradeVisibleButNotExecutable()
    {
        var session = BusinessTestSessionFactory.Create();
        Assert.Equal(
            StoreGrowthCommandStatus.Success,
            session.Game.UpgradeStore("store-1", StoreUpgradeKind.Shelf).Status);
        var snapshot = session.Simulation.GetSnapshot();

        var shelf = StoreInvestmentAdvisor.Create(
                snapshot,
                session.Game.GetStoreGrowthSnapshot("store-1"),
                StoreEconomyAnalysisService.Calculate(snapshot, "store-1")!)
            .Candidates.Single(candidate => candidate.Id == "growth:shelf");

        Assert.Equal(InvestmentAvailability.PrerequisiteNotMet, shelf.Availability);
        Assert.False(shelf.IsExecutable);
    }

    [Fact]
    public void Create_OmitsMaximumLevelUpgradeAndLeavesUnprovenPaybackUnknown()
    {
        var session = BusinessTestSessionFactory.Create();
        var snapshot = session.Simulation.GetSnapshot();
        var current = session.Game.GetStoreGrowthSnapshot("store-1");
        var maximumExpansion = current with
        {
            ExpansionLevel = 4,
            NextExpansionUpgradeCostCents = null
        };

        var portfolio = StoreInvestmentAdvisor.Create(
            snapshot,
            maximumExpansion,
            StoreEconomyAnalysisService.Calculate(snapshot, "store-1")!);

        Assert.DoesNotContain(portfolio.Candidates, candidate => candidate.Id == "growth:expansion");
        Assert.All(portfolio.Candidates, candidate => Assert.Null(candidate.Return.PaybackDaysTenths));
    }
}
