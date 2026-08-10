using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Business.Offline;

public sealed class ReturnBriefingServiceTests
{
    [Fact]
    public void Create_PrioritizesRecoveryForTheWeakestLossMakingStore()
    {
        var snapshot = SnapshotWithDay(
            new StoreDayReport(
                "store-1", 20, 15, 15, 0, 3_000, 1_500, 400, 800, 900, 0, 300),
            new StoreDayReport(
                "store-2", 12, 8, 8, 0, 1_600, 800, 1_200, -700, 900, 0, 300));
        var settlement = Settlement(
            appliedSeconds: 1_440,
            before: new OfflineBusinessTotals(100_000, 0, 0, 0, 0, 0),
            after: new OfflineBusinessTotals(98_000, 4_600, 2_300, 1_600, 100, 23),
            snapshot.LastCompletedDay);

        var briefing = ReturnBriefingService.Create(settlement, snapshot);

        Assert.True(briefing.IsVisible);
        Assert.Equal(1_440, briefing.AppliedSeconds);
        Assert.Equal(-2_000, briefing.CashDeltaCents);
        Assert.Equal(23, briefing.CompletedSalesDelta);
        Assert.Equal(100, briefing.NetProfitDeltaCents);
        Assert.Equal("store-2", briefing.AttentionStoreId);
        Assert.Equal(ReturnBriefingPriority.Recovery, briefing.Priority);
        Assert.Equal(StoreBottleneck.Cost, briefing.Bottleneck);
    }

    [Fact]
    public void Create_HidesWhenNoOfflineTimeWasApplied()
    {
        var snapshot = BusinessTestSessionFactory.Create().Simulation.GetSnapshot();
        var totals = new OfflineBusinessTotals(100_000, 0, 0, 0, 0, 0);

        var briefing = ReturnBriefingService.Create(
            Settlement(0, totals, totals, lastCompletedDay: null),
            snapshot);

        Assert.False(briefing.IsVisible);
    }

    [Fact]
    public void Create_PrioritizesReinvestmentAfterAProfitableCashPositiveReturn()
    {
        var snapshot = SnapshotWithDay(
            new StoreDayReport(
                "store-1", 20, 15, 15, 0, 3_000, 1_800, 400, 1_100, 900, 0, 300));
        var settlement = Settlement(
            1_440,
            new OfflineBusinessTotals(100_000, 0, 0, 0, 0, 0),
            new OfflineBusinessTotals(101_100, 3_000, 1_800, 400, 1_100, 15),
            snapshot.LastCompletedDay);

        var briefing = ReturnBriefingService.Create(settlement, snapshot);

        Assert.Equal(ReturnBriefingPriority.Reinvest, briefing.Priority);
        Assert.Equal("store-1", briefing.AttentionStoreId);
    }

    [Fact]
    public void Create_ObservesWhenNoCompletedDayCanSupportAdvice()
    {
        var snapshot = BusinessTestSessionFactory.Create().Simulation.GetSnapshot();
        var settlement = Settlement(
            300,
            new OfflineBusinessTotals(100_000, 0, 0, 0, 0, 0),
            new OfflineBusinessTotals(100_100, 200, 100, 0, 100, 1),
            lastCompletedDay: null);

        var briefing = ReturnBriefingService.Create(settlement, snapshot);

        Assert.True(briefing.IsVisible);
        Assert.Equal(ReturnBriefingPriority.Observe, briefing.Priority);
        Assert.Null(briefing.AttentionStoreId);
        Assert.Equal(StoreBottleneck.InsufficientData, briefing.Bottleneck);
    }

    private static BusinessSimulationSnapshot SnapshotWithDay(params StoreDayReport[] stores)
    {
        var session = BusinessTestSessionFactory.Create(openSecondStore: stores.Length > 1);
        var snapshot = session.Simulation.GetSnapshot();
        return snapshot with
        {
            Stores = snapshot.Stores
                .Select(store => store with
                {
                    CheckoutQueueLength = 0,
                    ServicePermille = 1_000
                })
                .ToArray(),
            LastCompletedDay = new BusinessDayReport(1, stores)
        };
    }

    private static OfflineSettlementResult Settlement(
        int appliedSeconds,
        OfflineBusinessTotals before,
        OfflineBusinessTotals after,
        BusinessDayReport? lastCompletedDay) =>
        new(
            appliedSeconds,
            appliedSeconds,
            WasCapped: false,
            OfflineTimeAnomaly.None,
            before,
            after,
            lastCompletedDay);
}
