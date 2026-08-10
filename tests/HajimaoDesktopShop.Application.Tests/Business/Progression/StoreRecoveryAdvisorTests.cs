using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Tests.Business.Progression;

public sealed class StoreRecoveryAdvisorTests
{
    [Theory]
    [InlineData(StoreBottleneck.Stock, -1, 20, StorePricingPreset.HighTurnover, "negative-profit")]
    [InlineData(StoreBottleneck.Demand, 1_000, 5, StorePricingPreset.HighTurnover, "critical-cash-runway")]
    [InlineData(StoreBottleneck.Cost, -1, 20, StorePricingPreset.Balanced, "negative-profit")]
    public void Create_ConvertsFinancialDangerIntoAnExistingLeanStrategy(
        StoreBottleneck bottleneck,
        long netProfitCents,
        int cashRunwayTenthsOfDay,
        StorePricingPreset expectedPricing,
        string expectedEvidenceCode)
    {
        var recommendation = StoreRecoveryAdvisor.Create(Analysis(
            bottleneck,
            netProfitCents,
            cashRunwayTenthsOfDay));

        Assert.NotNull(recommendation);
        Assert.Equal("store-1", recommendation.StoreId);
        Assert.Equal(bottleneck, recommendation.Bottleneck);
        Assert.Equal(expectedPricing, recommendation.Pricing);
        Assert.Equal(StoreStockingPreset.Lean, recommendation.Stocking);
        Assert.Equal(expectedEvidenceCode, recommendation.EvidenceCode);
    }

    [Fact]
    public void Create_ReturnsNullForHealthyProfitableStore()
    {
        var recommendation = StoreRecoveryAdvisor.Create(Analysis(
            StoreBottleneck.None,
            netProfitCents: 5_000,
            cashRunwayTenthsOfDay: 25));

        Assert.Null(recommendation);
    }

    private static StoreEconomyAnalysis Analysis(
        StoreBottleneck bottleneck,
        long netProfitCents,
        int cashRunwayTenthsOfDay) =>
        new(
            "store-1",
            "CompletedDay",
            RevenueCents: 20_000,
            GrossProfitCents: 8_000,
            WageCostCents: 4_000,
            OperatingCostCents: 2_000,
            netProfitCents,
            NecessaryOutflowCents: 18_000,
            GrossMarginBasisPoints: 4_000,
            NetMarginBasisPoints: 1_000,
            cashRunwayTenthsOfDay,
            Visitors: 20,
            CompletedSales: 10,
            LostSales: 2,
            bottleneck);
}
