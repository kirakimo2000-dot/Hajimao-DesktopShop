using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class PlayableDemoCandidateTests
{
    [Fact]
    public void ProductionLoop_ConnectsInvestmentReturnOfflineGrowthAndWeakStoreCapital()
    {
        var session = LongTermProgressionScenarioRunner.CreateSession(seed: 8_102);
        Assert.Equal(
            StoreStrategyCommandStatus.Success,
            session.Strategy.Apply(
                DesktopGameContent.StarterStoreId,
                StorePricingPreset.HighMargin,
                StoreStockingPreset.Lean).Status);
        session.Simulation.AdvanceRealSeconds(1_440);

        var firstInvestment = session.Investments.GetCapitalAllocation().Options
            .First(option => option.Thesis == CapitalAllocationThesis.StabilizeWeakestStore);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute(
                firstInvestment.ExecutionStoreId,
                firstInvestment.Candidate.Id).Status);
        session.Simulation.AdvanceRealSeconds(1_440);
        Assert.Equal(
            InvestmentComparisonStatus.Compared,
            session.Investments.GetLatestComparison(firstInvestment.Candidate.StoreId)?.Status);

        var onboarding = OnboardingProgressService.CreateSnapshot(
            session.Simulation.GetSnapshot(),
            session.Game.GetProcurementSnapshot(),
            session.Investments.HasAnyInvestment,
            hasComparableInvestmentReturn: true);
        Assert.True(onboarding.IsComplete);

        var savedAt = new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);
        for (var returnNumber = 0; returnNumber < 3; returnNumber++)
        {
            var save = session.CaptureSaveData(savedAt);
            savedAt = savedAt.AddSeconds(1_440);
            var restored = DesktopBusinessSessionFactory.Create(
                LongTermProgressionScenarioRunner.ProductionProducts,
                save,
                seed: 100 + returnNumber,
                nowUtc: savedAt,
                new OfflineSettlementPolicy(maxOfflineSeconds: 1_440, batchSize: 137));
            var settlement = Assert.IsType<OfflineSettlementResult>(restored.OfflineSettlement);
            Assert.True(ReturnBriefingService.Create(
                settlement,
                restored.Session.Simulation.GetSnapshot()).IsVisible);
            session = restored.Session;
        }

        var expansion = WaitForExecutableExpansion(session, maximumDays: 30);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute(
                expansion.ExecutionStoreId,
                expansion.Candidate.Id).Status);
        Assert.Equal(
            StoreStrategyCommandStatus.Success,
            session.Strategy.Apply(
                "station-store",
                StorePricingPreset.HighTurnover,
                StoreStockingPreset.Lean).Status);

        var (capitalBeforeWeakStoreInvestment, weakStoreInvestment) =
            WaitForWeakStoreInvestment(session, "station-store", maximumDays: 20);
        Assert.InRange(capitalBeforeWeakStoreInvestment.Options.Count, 1, 3);
        Assert.True(capitalBeforeWeakStoreInvestment.Options
            .Select(option => option.Thesis)
            .Distinct()
            .Count() >= 2);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute(
                weakStoreInvestment.ExecutionStoreId,
                weakStoreInvestment.Candidate.Id).Status);

        var simulation = session.Simulation.GetSnapshot();
        var catalog = session.Game.GetStoreCatalogSnapshot();
        var progression = LongTermProgressionService.Create(
            simulation,
            catalog,
            simulation.Business.Stores
                .Select(store => session.Game.GetStoreGrowthSnapshot(store.Id))
                .ToArray(),
            session.Investments.HasAnyInvestment);

        Assert.True(simulation.Business.CashCents >= 0);
        Assert.Equal(0, simulation.Stores.Sum(store => store.WagePaymentFailures));
        Assert.Equal(2, simulation.Business.Stores.Count);
        Assert.True(progression.OpenStoreCount < progression.ConfiguredStoreCount);
        Assert.DoesNotContain(
            typeof(BusinessSimulationSnapshot).GetProperties(),
            property => property.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase));
    }

    private static CapitalAllocationOption WaitForExecutableExpansion(
        BusinessSession session,
        int maximumDays)
    {
        for (var day = 0; day <= maximumDays; day++)
        {
            var expansion = session.Investments.GetCapitalAllocation().Options
                .FirstOrDefault(option => option.Thesis == CapitalAllocationThesis.ExpandStreet
                    && option.Candidate.IsExecutable);
            if (expansion is not null)
            {
                return expansion;
            }

            session.Simulation.AdvanceRealSeconds(1_440);
        }

        throw new Xunit.Sdk.XunitException("A safely executable second-store route did not appear.");
    }

    private static (CapitalAllocationSnapshot Capital, CapitalAllocationOption Investment)
        WaitForWeakStoreInvestment(
            BusinessSession session,
            string storeId,
            int maximumDays)
    {
        for (var day = 0; day <= maximumDays; day++)
        {
            var capital = session.Investments.GetCapitalAllocation();
            var investment = capital.Options.FirstOrDefault(option =>
                option.Thesis == CapitalAllocationThesis.StabilizeWeakestStore
                && option.Candidate.StoreId == storeId
                && option.Candidate.IsExecutable);
            if (investment is not null)
            {
                return (capital, investment);
            }

            session.Simulation.AdvanceRealSeconds(1_440);
        }

        throw new Xunit.Sdk.XunitException("The newly opened weak store did not receive capital advice.");
    }
}
