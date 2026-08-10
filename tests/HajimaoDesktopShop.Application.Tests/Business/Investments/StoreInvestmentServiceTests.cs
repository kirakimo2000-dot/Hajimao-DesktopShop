using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Tests.Business;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class StoreInvestmentServiceTests
{
    [Fact]
    public void Execute_GrowthCandidateUsesExistingUpgradeCommandForSelectedStoreOnly()
    {
        var session = BusinessTestSessionFactory.Create(
            openSecondStore: true,
            openingCashCents: 500_000);
        var otherBefore = session.Game.GetStoreGrowthSnapshot("store-2");

        var result = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(InvestmentKind.Shelf, result.AppliedCandidate?.Kind);
        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
        Assert.Equal(otherBefore, session.Game.GetStoreGrowthSnapshot("store-2"));
        var tracking = session.Investments.GetLatestComparison("store-1");
        Assert.NotNull(tracking);
        Assert.Equal("growth:shelf", tracking.CandidateId);
        Assert.Equal(InvestmentComparisonStatus.BaselineUnavailable, tracking.Status);
    }

    [Fact]
    public void Execute_EmployeeCandidateUsesExistingHireCommand()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .First(item => item.Kind == InvestmentKind.Employee);

        var result = session.Investments.Execute("store-1", candidate.Id);

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(candidate.Return.CostCents, result.CostCents);
        Assert.Contains(
            session.Simulation.GetSnapshot().Employees.Employees,
            employee => employee.EmployeeId == result.CreatedEmployeeId
                && employee.StoreId == "store-1");
        Assert.DoesNotContain(
            session.Simulation.GetSnapshot().Employees.Candidates,
            item => item.CandidateId == candidate.TargetId);
        Assert.Equal(candidate.Id, session.Investments.GetLatestComparison("store-1")?.CandidateId);
    }

    [Fact]
    public void Execute_UnknownOrStaleCandidateDoesNotChangeCash()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .First(item => item.Kind == InvestmentKind.Employee);

        var first = session.Investments.Execute("store-1", candidate.Id);
        var cashAfterFirst = session.Game.GetSnapshot().CashCents;
        var stale = session.Investments.Execute("store-1", candidate.Id);
        var missing = session.Investments.Execute("store-1", "missing");

        Assert.Equal(InvestmentCommandStatus.Success, first.Status);
        Assert.Equal(InvestmentCommandStatus.UnknownCandidate, stale.Status);
        Assert.Equal(InvestmentCommandStatus.UnknownCandidate, missing.Status);
        Assert.Equal(cashAfterFirst, session.Game.GetSnapshot().CashCents);
    }

    [Fact]
    public void Execute_RejectsUnknownStoreAndUnaffordableCandidateAtomically()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 10_000);
        var cashBefore = session.Game.GetSnapshot().CashCents;

        var unknownStore = session.Investments.Execute("missing-store", "growth:shelf");
        var unaffordable = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.UnknownStore, unknownStore.Status);
        Assert.Equal(InvestmentCommandStatus.InsufficientFunds, unaffordable.Status);
        Assert.Equal(cashBefore, session.Game.GetSnapshot().CashCents);
        Assert.Equal(0, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
    }

    [Fact]
    public void Execute_RevalidatesPrerequisiteImmediatelyBeforeDispatch()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute("store-1", "growth:shelf").Status);

        var blocked = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.NotAvailable, blocked.Status);
        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
    }
}
