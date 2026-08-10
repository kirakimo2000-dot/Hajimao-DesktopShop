using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class InvestmentPortfolioViewModelTests
{
    [Fact]
    public void Refresh_FormatsCalculatedCandidateWithoutOwningReturnFormula()
    {
        var session = MarketTestSession.Create();
        var viewModel = new InvestmentPortfolioViewModel(
            session,
            () => "corner-store",
            refreshMarket: () => { });

        var shelf = viewModel.Candidates.Single(candidate => candidate.Id == "growth:shelf");

        Assert.Equal("升级货架", shelf.TitleText);
        Assert.Equal("投入 ¥250.00", shelf.CostText);
        Assert.Equal("暂无足够数据", shelf.ExpectedBenefitText);
        Assert.Equal("等待经营证据", shelf.PaybackText);
        Assert.Equal("投资后现金 ¥750.00", shelf.CashAfterText);
        Assert.Equal("缺少完整支出基准", shelf.CashPressureText);
        Assert.Equal("库存容量 +25%", shelf.EffectText);
        Assert.Equal("当前数据不足，暂不估算收益", shelf.EstimateConditionText);
        Assert.True(shelf.InvestCommand.CanExecute(null));
    }

    [Fact]
    public void InvestCommand_ExecutesUnifiedCandidateAndShowsLatestComparisonState()
    {
        var session = MarketTestSession.Create();
        var refreshCount = 0;
        var viewModel = new InvestmentPortfolioViewModel(
            session,
            () => "corner-store",
            () => refreshCount++);

        viewModel.Candidates.Single(candidate => candidate.Id == "growth:shelf")
            .InvestCommand.Execute(null);

        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("corner-store").ShelfLevel);
        Assert.Equal(1, refreshCount);
        Assert.True(viewModel.HasLatestInvestment);
        Assert.Equal("最近投资：升级货架", viewModel.LatestInvestmentTitle);
        Assert.Contains("投资前没有完整日结", viewModel.LatestComparisonText);
        Assert.Contains("投资成功", viewModel.StatusMessage);
    }

    [Fact]
    public void Refresh_DisablesUnaffordableCandidateAndExplainsPressure()
    {
        var viewModel = new InvestmentPortfolioViewModel(
            MarketTestSession.Create(openingCashCents: 10_000),
            () => "corner-store",
            refreshMarket: () => { });

        var shelf = viewModel.Candidates.Single(candidate => candidate.Id == "growth:shelf");

        Assert.False(shelf.InvestCommand.CanExecute(null));
        Assert.Equal("资金不足", shelf.AvailabilityText);
        Assert.Equal("无法支付", shelf.CashPressureText);
    }
}
