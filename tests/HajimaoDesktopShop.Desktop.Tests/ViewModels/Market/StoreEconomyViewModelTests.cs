using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreEconomyViewModelTests
{
    [Fact]
    public void Update_FormatsOnlyCalculatedEconomyAnalysis()
    {
        var analysis = new StoreEconomyAnalysis(
            "corner-store",
            "CompletedDay",
            RevenueCents: 100_000,
            GrossProfitCents: 40_000,
            WageCostCents: 15_000,
            OperatingCostCents: 5_000,
            NetProfitCents: 20_000,
            NecessaryOutflowCents: 80_000,
            GrossMarginBasisPoints: 4_000,
            NetMarginBasisPoints: 2_000,
            CashRunwayTenthsOfDay: 15,
            Visitors: 100,
            CompletedSales: 80,
            LostSales: 4,
            StoreBottleneck.Stock);
        var viewModel = new StoreEconomyViewModel();

        viewModel.Update(analysis);

        Assert.Equal("昨日经营", viewModel.PeriodText);
        Assert.Equal("¥1,000.00", viewModel.RevenueText);
        Assert.Equal("¥400.00 · 40.0%", viewModel.GrossProfitText);
        Assert.Equal("¥150.00", viewModel.WageCostText);
        Assert.Equal("¥50.00", viewModel.OperatingCostText);
        Assert.Equal("¥200.00 · 20.0%", viewModel.NetProfitText);
        Assert.Equal("1.5 天", viewModel.CashRunwayText);
        Assert.Equal("顾客 100 · 成交 80 · 流失 4", viewModel.CustomerFlowText);
        Assert.Equal("库存不足正在损失订单", viewModel.BottleneckText);
        Assert.Equal("昨日净赚 ¥200.00", viewModel.PerformanceHeadlineText);
        Assert.Equal(
            "收入 ¥1,000.00 · 毛利 ¥400.00 · 40.0% · 顾客 100 · 成交 80 · 流失 4",
            viewModel.PerformanceDetailText);
        Assert.Equal("主要原因：库存不足正在损失订单", viewModel.ReasonHeadlineText);
        Assert.Equal(
            "工资 ¥150.00 · 运营成本 ¥50.00 · 现金续航 1.5 天",
            viewModel.ReasonDetailText);
    }

    [Theory]
    [InlineData(-20_000, "昨日亏损 ¥200.00")]
    [InlineData(0, "昨日盈亏平衡")]
    public void Update_ExplainsLossOrBreakEvenWithoutExposingMoreControls(
        long netProfitCents,
        string expectedHeadline)
    {
        var analysis = new StoreEconomyAnalysis(
            "corner-store",
            "CompletedDay",
            RevenueCents: 100_000,
            GrossProfitCents: 20_000,
            WageCostCents: 15_000,
            OperatingCostCents: 5_000,
            NetProfitCents: netProfitCents,
            NecessaryOutflowCents: 80_000,
            GrossMarginBasisPoints: 2_000,
            NetMarginBasisPoints: 0,
            CashRunwayTenthsOfDay: 15,
            Visitors: 100,
            CompletedSales: 80,
            LostSales: 4,
            StoreBottleneck.Cost);
        var viewModel = new StoreEconomyViewModel();

        viewModel.Update(analysis);

        Assert.Equal(expectedHeadline, viewModel.PerformanceHeadlineText);
    }
}
