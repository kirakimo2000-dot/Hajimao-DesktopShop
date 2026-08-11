using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class LongTermActiveIdlePlayableLoopTests
{
    [Fact]
    public void ProductionContent_ProgressesWhileRunningAndLeavesALaterGoal()
    {
        var session = LongTermProgressionScenarioRunner.CreateSession(seed: 8_101);
        Assert.Equal(
            StoreStrategyCommandStatus.Success,
            session.Strategy.Apply(
                DesktopGameContent.StarterStoreId,
                StorePricingPreset.Balanced,
                StoreStockingPreset.Lean).Status);
        session.Simulation.AdvanceRealSeconds(1_440);
        Assert.True(session.Simulation.GetSnapshot().LastCompletedDay?.Stores.Sum(
            store => store.NetProfitCents) > 0);

        var firstInvestment = session.Investments.GetPortfolio(DesktopGameContent.StarterStoreId)!
            .Candidates.Single(candidate => candidate.Kind == InvestmentKind.Shelf);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute(
                DesktopGameContent.StarterStoreId,
                firstInvestment.Id).Status);

        AdvanceActiveDay(session);
        while (session.Game.GetSnapshot().PlayerLevel < 3
               || session.Game.GetSnapshot().CashCents < 100_000)
        {
            AdvanceActiveDay(session);
        }

        var opening = session.Investments.GetPortfolio(DesktopGameContent.StarterStoreId)!
            .Candidates.Single(candidate => candidate.Kind == InvestmentKind.OpenStore
                && candidate.TargetId == "station-store");
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute(DesktopGameContent.StarterStoreId, opening.Id).Status);

        var employee = FindCashier(session);
        var staffingWaitDays = 0;
        while (employee is null
               || session.Game.GetSnapshot().CashCents < employee.Return.CostCents + 25_000)
        {
            Assert.True(staffingWaitDays++ < 30, "A cashier route did not become safely affordable.");
            AdvanceActiveDay(session);
            employee = FindCashier(session);
        }

        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute("station-store", employee.Id).Status);
        Assert.Equal(
            StoreStrategyCommandStatus.Success,
            session.Strategy.Apply(
                "station-store",
                StorePricingPreset.HighTurnover,
                StoreStockingPreset.Lean).Status);

        var recoveryDays = 0;
        do
        {
            Assert.True(recoveryDays++ < 30, "The staffed two-store portfolio did not return to profit.");
            AdvanceActiveDay(session);
        }
        while (session.Simulation.GetSnapshot().LastCompletedDay?.Stores.Sum(
            store => store.NetProfitCents) <= 0);

        var snapshot = session.Simulation.GetSnapshot();
        var tracking = session.Investments.CaptureTrackingSaveData();
        var catalog = session.Game.GetStoreCatalogSnapshot();
        var progression = LongTermProgressionService.Create(
            snapshot,
            catalog,
            snapshot.Business.Stores
                .Select(store => session.Game.GetStoreGrowthSnapshot(store.Id))
                .ToArray(),
            session.Investments.HasAnyInvestment);

        Assert.True(snapshot.Business.CashCents >= 0);
        Assert.Equal(2, snapshot.Business.Stores.Count);
        Assert.Equal(2, snapshot.Stores.Count);
        Assert.Equal(
            [DesktopGameContent.StarterStoreId, "station-store"],
            tracking.LatestInvestments.Select(investment => investment.StoreId));
        Assert.Equal(ProgressionGoalId.StrengthenPortfolio, progression.CurrentGoal.Id);
        Assert.True(progression.CurrentGoal.TargetValue > progression.CurrentGoal.CurrentValue);
        Assert.Equal(0, snapshot.GameMinute % 1_440);
        Assert.DoesNotContain(
            typeof(BusinessSimulationSnapshot).GetProperties(),
            property => property.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase));
        Assert.All(snapshot.Stores, store => Assert.Equal(0, store.WagePaymentFailures));
    }

    private static void AdvanceActiveDay(BusinessSession session) =>
        session.Simulation.AdvanceRealSeconds(1_440);

    private static InvestmentCandidate? FindCashier(
        BusinessSession session) =>
        session.Investments.GetPortfolio("station-store")!.Candidates
            .Where(candidate => candidate.Kind == InvestmentKind.Employee
                && candidate.Effect.AddedRole == EmployeeRole.Cashier)
            .OrderBy(candidate => candidate.Return.CostCents)
            .FirstOrDefault();
}
