using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Tests.Business;

namespace HajimaoDesktopShop.Application.Tests.Business.Analysis;

public sealed class StoreEconomyAnalysisServiceTests
{
    [Fact]
    public void Calculate_ComputesMarginsAndCashRunwayFromCompletedDay()
    {
        var input = Input(
            sharedCashCents: 120_000,
            revenueCents: 100_000,
            grossProfitCents: 40_000,
            wageCostCents: 15_000,
            operatingCostCents: 5_000,
            netProfitCents: 20_000,
            visitors: 100,
            completedSales: 80,
            isCompletedDay: true);

        var analysis = StoreEconomyAnalysisService.Calculate(input);

        Assert.Equal("CompletedDay", analysis.Period);
        Assert.Equal(4_000, analysis.GrossMarginBasisPoints);
        Assert.Equal(2_000, analysis.NetMarginBasisPoints);
        Assert.Equal(15, analysis.CashRunwayTenthsOfDay);
        Assert.Equal(80_000, analysis.NecessaryOutflowCents);
        Assert.Equal(StoreBottleneck.None, analysis.Bottleneck);
    }

    [Fact]
    public void Calculate_FromSimulationSnapshot_PrefersLatestCompletedDay()
    {
        var session = BusinessTestSessionFactory.Create();
        var snapshot = session.Simulation.GetSnapshot() with
        {
            LastCompletedDay = new BusinessDayReport(
                3,
                [new StoreDayReport(
                    "store-1",
                    Visitors: 50,
                    AcceptedPurchases: 30,
                    CompletedSales: 20,
                    LostSales: 5,
                    RevenueCents: 10_000,
                    GrossProfitCents: 4_000,
                    WageCostCents: 1_000,
                    NetProfitCents: 2_500,
                    ClosingCleanlinessPermille: 900,
                    AverageQueueLengthBasisPoints: 0,
                    OperatingCostCents: 500)])
        };

        var analysis = StoreEconomyAnalysisService.Calculate(snapshot, "store-1");

        Assert.NotNull(analysis);
        Assert.Equal("CompletedDay", analysis.Period);
        Assert.Equal(7_500, analysis.NecessaryOutflowCents);
        Assert.Equal(20, analysis.CompletedSales);
        Assert.Equal(5, analysis.LostSales);
    }

    [Fact]
    public void Calculate_FromSimulationSnapshot_ReturnsNullForUnknownStore()
    {
        var snapshot = BusinessTestSessionFactory.Create().Simulation.GetSnapshot();

        var analysis = StoreEconomyAnalysisService.Calculate(snapshot, "missing-store");

        Assert.Null(analysis);
    }

    [Fact]
    public void Calculate_HandlesZeroRevenueBeforeFirstCompletedDay()
    {
        var analysis = StoreEconomyAnalysisService.Calculate(Input());

        Assert.Equal("SinceOpening", analysis.Period);
        Assert.Equal(0, analysis.GrossMarginBasisPoints);
        Assert.Equal(0, analysis.NetMarginBasisPoints);
        Assert.Equal(0, analysis.CashRunwayTenthsOfDay);
        Assert.Equal(StoreBottleneck.InsufficientData, analysis.Bottleneck);
    }

    [Theory]
    [InlineData(1, 1, 900, 1, 1, 10_000, StoreBottleneck.Stock)]
    [InlineData(0, 3, 900, 1, 1, 10_000, StoreBottleneck.Checkout)]
    [InlineData(0, 0, 699, 0, 1, 10_000, StoreBottleneck.Service)]
    [InlineData(0, 0, 900, 0, 1, -1, StoreBottleneck.Cost)]
    [InlineData(0, 0, 900, 0, 0, 0, StoreBottleneck.Demand)]
    public void Calculate_UsesDeterministicBottleneckPriority(
        int outOfStockProductCount,
        int checkoutQueueLength,
        int servicePermille,
        int lostSales,
        int completedSales,
        long netProfitCents,
        StoreBottleneck expected)
    {
        var analysis = StoreEconomyAnalysisService.Calculate(Input(
            sharedCashCents: 20_000,
            revenueCents: 10_000,
            grossProfitCents: 4_000,
            wageCostCents: 1_000,
            operatingCostCents: 500,
            netProfitCents: netProfitCents,
            visitors: 10,
            completedSales: completedSales,
            lostSales: lostSales,
            outOfStockProductCount: outOfStockProductCount,
            checkoutQueueLength: checkoutQueueLength,
            servicePermille: servicePermille));

        Assert.Equal(expected, analysis.Bottleneck);
    }

    [Fact]
    public void Calculate_SaturatesRatioOutputsAfterIntegerCalculation()
    {
        var analysis = StoreEconomyAnalysisService.Calculate(Input(
            sharedCashCents: int.MaxValue,
            revenueCents: 1,
            grossProfitCents: 1_000_000,
            netProfitCents: -1_000_000,
            visitors: 1,
            completedSales: 1));

        Assert.Equal(int.MaxValue, analysis.GrossMarginBasisPoints);
        Assert.Equal(int.MinValue, analysis.NetMarginBasisPoints);
    }

    private static StoreEconomyAnalysisInput Input(
        long sharedCashCents = 0,
        long revenueCents = 0,
        long grossProfitCents = 0,
        long wageCostCents = 0,
        long operatingCostCents = 0,
        long netProfitCents = 0,
        int visitors = 0,
        int completedSales = 0,
        int lostSales = 0,
        int outOfStockProductCount = 0,
        int checkoutQueueLength = 0,
        int servicePermille = 900,
        bool isCompletedDay = false) =>
        new(
            StoreId: "store-1",
            SharedCashCents: sharedCashCents,
            RevenueCents: revenueCents,
            GrossProfitCents: grossProfitCents,
            WageCostCents: wageCostCents,
            OperatingCostCents: operatingCostCents,
            NetProfitCents: netProfitCents,
            Visitors: visitors,
            CompletedSales: completedSales,
            LostSales: lostSales,
            OutOfStockProductCount: outOfStockProductCount,
            CheckoutQueueLength: checkoutQueueLength,
            ServicePermille: servicePermille,
            IsCompletedDay: isCompletedDay);
}
