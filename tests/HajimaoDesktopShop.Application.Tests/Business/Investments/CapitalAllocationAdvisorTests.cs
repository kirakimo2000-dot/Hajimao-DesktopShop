using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Analysis;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class CapitalAllocationAdvisorTests
{
    [Fact]
    public void Create_SelectsWeakStoreReturnAndExpansionWithoutDuplicatingOpenStore()
    {
        var openStore = Candidate(
            "store:open:store-3",
            "store-3",
            InvestmentKind.OpenStore,
            StoreBottleneck.Demand,
            costCents: 200_000,
            benefitCents: 0,
            paybackDaysTenths: null,
            availability: InvestmentAvailability.LevelLocked,
            targetId: "store-3");
        var strongReturn = Candidate(
            "growth:decoration",
            "store-1",
            InvestmentKind.Decoration,
            StoreBottleneck.Demand,
            costCents: 30_000,
            benefitCents: 3_000,
            paybackDaysTenths: 100);
        var weakShelf = Candidate(
            "growth:shelf",
            "store-2",
            InvestmentKind.Shelf,
            StoreBottleneck.Stock,
            costCents: 25_000,
            benefitCents: 1_000,
            paybackDaysTenths: 250);

        var snapshot = CapitalAllocationAdvisor.Create(
            [
                new StoreCatalogItemSnapshot("store-1", "街角店", 1, 0, true),
                new StoreCatalogItemSnapshot("store-2", "车站店", 3, 80_000, true),
                new StoreCatalogItemSnapshot("store-3", "社区店", 5, 200_000, false)
            ],
            [
                Portfolio("store-1", netProfitCents: 5_000, StoreBottleneck.None, strongReturn, openStore),
                Portfolio("store-2", netProfitCents: -500, StoreBottleneck.Stock, weakShelf, openStore)
            ]);

        Assert.Equal(3, snapshot.Options.Count);
        Assert.Equal(3, snapshot.Options.Select(option => option.Thesis).Distinct().Count());
        var stabilize = snapshot.Options.Single(option =>
            option.Thesis == CapitalAllocationThesis.StabilizeWeakestStore);
        Assert.Equal("store-2", stabilize.ExecutionStoreId);
        Assert.Equal("车站店", stabilize.StoreName);
        Assert.Equal(weakShelf, stabilize.Candidate);
        var improve = snapshot.Options.Single(option =>
            option.Thesis == CapitalAllocationThesis.ImproveReturn);
        Assert.Equal("store-1", improve.ExecutionStoreId);
        Assert.Equal(strongReturn, improve.Candidate);
        var expansion = snapshot.Options.Single(option =>
            option.Thesis == CapitalAllocationThesis.ExpandStreet);
        Assert.Equal("store-1", expansion.ExecutionStoreId);
        Assert.Equal("社区店", expansion.StoreName);
        Assert.Equal(openStore, expansion.Candidate);
        Assert.Single(snapshot.Options, option => option.Candidate.Kind == InvestmentKind.OpenStore);
    }

    private static StoreInvestmentPortfolio Portfolio(
        string storeId,
        long netProfitCents,
        StoreBottleneck bottleneck,
        params InvestmentCandidate[] candidates) =>
        new(
            storeId,
            new StoreEconomyAnalysis(
                storeId,
                "CompletedDay",
                RevenueCents: 10_000,
                GrossProfitCents: 5_000,
                WageCostCents: 2_000,
                OperatingCostCents: 500,
                NetProfitCents: netProfitCents,
                NecessaryOutflowCents: 7_500,
                GrossMarginBasisPoints: 5_000,
                NetMarginBasisPoints: 1_000,
                CashRunwayTenthsOfDay: 20,
                Visitors: 20,
                CompletedSales: 10,
                LostSales: 2,
                Bottleneck: bottleneck),
            Array.AsReadOnly(candidates));

    private static InvestmentCandidate Candidate(
        string id,
        string storeId,
        InvestmentKind kind,
        StoreBottleneck bottleneck,
        long costCents,
        long benefitCents,
        long? paybackDaysTenths,
        InvestmentAvailability availability = InvestmentAvailability.Available,
        string? targetId = null) =>
        new(
            id,
            storeId,
            kind,
            targetId ?? id,
            targetId ?? id,
            new InvestmentReturnEstimate(
                costCents,
                benefitCents,
                paybackDaysTenths,
                CashAfterInvestmentCents: 500_000 - costCents,
                InvestmentCashPressure.Healthy,
                IsAffordable: availability != InvestmentAvailability.InsufficientFunds),
            new InvestmentObservableEffect(),
            bottleneck,
            InvestmentEstimateCondition.InsufficientEvidence,
            availability,
            RequiredPlayerLevel: kind == InvestmentKind.OpenStore ? 5 : 0);
}
