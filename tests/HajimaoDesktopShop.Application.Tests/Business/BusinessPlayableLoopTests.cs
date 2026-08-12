using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Tests.Business;

public sealed class BusinessPlayableLoopTests
{
    [Fact]
    public void DefaultStore_CanOperateForSevenDaysWithoutMaintenanceCommands()
    {
        var session = BusinessTestSessionFactory.Create();

        for (var day = 0; day < 7; day++)
        {
            session.Simulation.AdvanceRealSeconds(1_440);
        }

        var snapshot = session.Simulation.GetSnapshot();
        Assert.True(snapshot.Business.Stores[0].RevenueCents > 0);
        Assert.DoesNotContain(snapshot.Stores, store => store.WagePaymentFailures > 0);
        Assert.True(snapshot.Business.CashCents >= 0);
    }

    [Fact]
    public void PricingStrategies_ProduceDifferentTurnoverAndMarginOutcomes()
    {
        var turnover = BusinessTestSessionFactory.Create(randomState: 456);
        var margin = BusinessTestSessionFactory.Create(randomState: 456);
        turnover.Strategy.Apply(
            "store-1",
            StorePricingPreset.HighTurnover,
            StoreStockingPreset.Balanced);
        margin.Strategy.Apply(
            "store-1",
            StorePricingPreset.HighMargin,
            StoreStockingPreset.Balanced);

        turnover.Simulation.AdvanceRealSeconds(2_880);
        margin.Simulation.AdvanceRealSeconds(2_880);

        var turnoverSnapshot = turnover.Simulation.GetSnapshot();
        var marginSnapshot = margin.Simulation.GetSnapshot();
        Assert.True(
            turnoverSnapshot.Stores[0].CompletedSales
            > marginSnapshot.Stores[0].CompletedSales);
        Assert.True(
            turnoverSnapshot.Business.Stores[0].Products[0].GrossMarginBasisPoints
            < marginSnapshot.Business.Stores[0].Products[0].GrossMarginBasisPoints);
    }

}
