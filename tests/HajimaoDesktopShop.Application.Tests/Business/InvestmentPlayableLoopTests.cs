using HajimaoDesktopShop.Application.Business.Investments;

namespace HajimaoDesktopShop.Application.Tests.Business;

public sealed class InvestmentPlayableLoopTests
{
    private static readonly DateTimeOffset SavedAt =
        new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OperatingStore_OffersDistinctComparableInvestmentRoutes()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 1_000_000);
        session.Simulation.AdvanceRealSeconds(1_440);

        var portfolio = Assert.IsType<StoreInvestmentPortfolio>(
            session.Investments.GetPortfolio("store-1"));
        var executableKinds = portfolio.Candidates
            .Where(candidate => candidate.IsExecutable)
            .Select(candidate => candidate.Kind)
            .Distinct()
            .ToArray();

        Assert.True(executableKinds.Length >= 3);
        Assert.Contains(InvestmentKind.Employee, executableKinds);
        Assert.All(portfolio.Candidates, candidate =>
        {
            if (candidate.Kind == InvestmentKind.OpenStore)
            {
                Assert.True(candidate.Return.CostCents >= 0);
            }
            else
            {
                Assert.True(candidate.Return.CostCents > 0);
            }

            Assert.False(string.IsNullOrWhiteSpace(candidate.TargetName));
            Assert.NotEqual(new InvestmentObservableEffect(), candidate.Effect);
        });
    }

    [Fact]
    public void Investment_SaveIdleDayAndRestore_ProducesActualDayComparison()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 1_000_000);
        session.Simulation.AdvanceRealSeconds(1_440);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .First(item => item.Kind == InvestmentKind.Shelf && item.IsExecutable);

        var result = session.Investments.Execute("store-1", candidate.Id);
        var restored = BusinessTestSessionFactory.Restore(session.CaptureSaveData(SavedAt));
        var restoredBeforeIdle = restored.Investments.GetLatestComparison("store-1");
        restored.Simulation.AdvanceRealSeconds(1_440);
        var compared = restored.Investments.GetLatestComparison("store-1");

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(InvestmentComparisonStatus.WaitingForCompletedDay, restoredBeforeIdle?.Status);
        Assert.Equal(InvestmentComparisonStatus.Compared, compared?.Status);
        Assert.Equal(candidate.Id, compared?.CandidateId);
        Assert.NotNull(compared?.NetProfitChangeCents);
        Assert.NotNull(compared?.CompletedSalesChange);
        Assert.NotNull(compared?.LostSalesChange);
    }
}
