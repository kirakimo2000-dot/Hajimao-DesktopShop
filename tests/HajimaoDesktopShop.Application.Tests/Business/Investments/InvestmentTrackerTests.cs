using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Tests.Business;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class InvestmentTrackerTests
{
    [Fact]
    public void GetSnapshot_WaitsThenComparesAgainstALaterCompletedDay()
    {
        var tracker = new InvestmentTracker();
        var candidate = Candidate();
        var baseline = Day(2, netProfitCents: 1_000, completedSales: 10, lostSales: 5);
        tracker.Record(candidate, gameMinute: 2_000, baseline);

        var waiting = tracker.GetSnapshot("store-1", baseline);
        var compared = tracker.GetSnapshot(
            "store-1",
            Day(3, netProfitCents: 3_000, completedSales: 14, lostSales: 2));

        Assert.NotNull(waiting);
        Assert.Equal(InvestmentComparisonStatus.WaitingForCompletedDay, waiting.Status);
        Assert.Null(waiting.NetProfitChangeCents);
        Assert.NotNull(compared);
        Assert.Equal(InvestmentComparisonStatus.Compared, compared.Status);
        Assert.Equal(2_000, compared.NetProfitChangeCents);
        Assert.Equal(4, compared.CompletedSalesChange);
        Assert.Equal(-3, compared.LostSalesChange);
    }

    [Fact]
    public void GetSnapshot_ReportsUnavailableBaselineWithoutInventingComparison()
    {
        var tracker = new InvestmentTracker();
        tracker.Record(Candidate(), gameMinute: 10, baselineDay: null);

        var snapshot = tracker.GetSnapshot(
            "store-1",
            Day(1, netProfitCents: 3_000, completedSales: 14, lostSales: 2));

        Assert.NotNull(snapshot);
        Assert.Equal(InvestmentComparisonStatus.BaselineUnavailable, snapshot.Status);
        Assert.Null(snapshot.NetProfitChangeCents);
    }

    [Fact]
    public void Record_ReplacesOnlyTheSameStoresLatestInvestmentAndRoundTripsState()
    {
        var tracker = new InvestmentTracker();
        var first = Candidate();
        var replacement = first with { Id = "growth:decoration", Kind = InvestmentKind.Decoration };
        var other = first with { Id = "growth:expansion", StoreId = "store-2", Kind = InvestmentKind.Expansion };
        tracker.Record(first, 10, Day(1, 100, 2, 1));
        tracker.Record(other, 11, Day(1, 200, 3, 1));
        tracker.Record(replacement, 12, Day(1, 300, 4, 1));

        var restored = new InvestmentTracker(tracker.CaptureSaveData());

        Assert.Equal("growth:decoration", restored.GetSnapshot("store-1", null)?.CandidateId);
        Assert.Equal("growth:expansion", restored.GetSnapshot("store-2", null)?.CandidateId);
        Assert.Equal(2, restored.CaptureSaveData().LatestInvestments.Count);
    }

    private static InvestmentCandidate Candidate()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        return session.Investments.GetPortfolio("store-1")!.Candidates
            .Single(candidate => candidate.Id == "growth:shelf");
    }

    private static BusinessDayReport Day(
        int dayNumber,
        long netProfitCents,
        int completedSales,
        int lostSales) =>
        new(
            dayNumber,
            [new StoreDayReport(
                "store-1",
                Visitors: completedSales + lostSales,
                AcceptedPurchases: completedSales,
                CompletedSales: completedSales,
                LostSales: lostSales,
                RevenueCents: completedSales * 200L,
                GrossProfitCents: completedSales * 100L,
                WageCostCents: 0,
                NetProfitCents: netProfitCents,
                ClosingCleanlinessPermille: 900,
                AverageQueueLengthBasisPoints: 0)]);
}
