using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class InvestmentPortfolioViewModelTests
{
    [Fact]
    public void Refresh_PresentsAtMostThreePortfolioLevelCapitalChoices()
    {
        var viewModel = new InvestmentPortfolioViewModel(
            MarketTestSession.Create(),
            () => "corner-store",
            refreshMarket: () => { });

        Assert.InRange(viewModel.Candidates.Count, 1, 3);
    }

    [Fact]
    public void Refresh_FormatsCalculatedCandidateWithoutOwningReturnFormula()
    {
        var session = MarketTestSession.Create();
        var viewModel = new InvestmentPortfolioViewModel(
            session,
            () => "corner-store",
            refreshMarket: () => { });

        var shelf = viewModel.Candidates.Single(candidate => candidate.Id == "growth:shelf");

        Assert.Equal("稳住弱店", shelf.ThesisText);
        Assert.Equal("7-Eleven", shelf.StoreContextText);
        Assert.Equal("升级货架", shelf.TitleText);
        Assert.Equal("投入 ¥250.00", shelf.CostText);
        Assert.Equal("暂无足够数据", shelf.ExpectedBenefitText);
        Assert.Equal("等待经营证据", shelf.PaybackText);
        Assert.Equal("投资后现金 ¥750.00", shelf.CashAfterText);
        Assert.Equal("缺少完整支出基准", shelf.CashPressureText);
        Assert.Equal("库存容量 +25%", shelf.EffectText);
        Assert.Equal("当前数据不足，暂不估算收益", shelf.EstimateConditionText);
        Assert.True(shelf.InvestCommand.CanExecute(null));
        Assert.Equal("7-Eleven · 升级货架", viewModel.NextInvestmentTitle);
        Assert.Equal(
            "投入 ¥250.00 · 暂无足够数据 · 缺少完整支出基准",
            viewModel.NextInvestmentDetailText);
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
    public void InvestCommand_UsesTheAdvisorsExecutionStoreInsteadOfSelectedStore()
    {
        var session = MarketTestSession.Create();
        var viewModel = new InvestmentPortfolioViewModel(
            session,
            () => "station-store",
            refreshMarket: () => { });

        viewModel.Candidates.Single(candidate => candidate.Id == "growth:shelf")
            .InvestCommand.Execute(null);

        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("corner-store").ShelfLevel);
        Assert.DoesNotContain(
            session.Game.GetSnapshot().Stores,
            store => store.Id == "station-store");
    }

    [Fact]
    public void Refresh_HidesUnaffordableOperatingMovesInsteadOfFillingThePage()
    {
        var viewModel = new InvestmentPortfolioViewModel(
            MarketTestSession.Create(openingCashCents: 10_000),
            () => "corner-store",
            refreshMarket: () => { });

        var opening = Assert.Single(viewModel.Candidates);

        Assert.Equal("扩张街区", opening.ThesisText);
        Assert.Equal("store:open:station-store", opening.Id);
        Assert.False(opening.InvestCommand.CanExecute(null));
    }

    [Fact]
    public void Refresh_FormatsStoreOpeningAsACashGatedInvestment()
    {
        var viewModel = new InvestmentPortfolioViewModel(
            MarketTestSession.Create(),
            () => "corner-store",
            refreshMarket: () => { });

        var opening = viewModel.Candidates.Single(candidate =>
            candidate.Id == "store:open:station-store");

        Assert.Equal("扩张街区", opening.ThesisText);
        Assert.Equal("FamilyMart", opening.StoreContextText);
        Assert.Equal("开设 FamilyMart", opening.TitleText);
        Assert.Equal("投入 ¥800.00", opening.CostText);
        Assert.Equal("新店尚无完整经营数据", opening.ExpectedBenefitText);
        Assert.Equal("新店日结后评估回本", opening.PaybackText);
        Assert.Equal("新增店铺 +1", opening.EffectText);
        Assert.Equal("新店需完成一个经营日后再评估回报", opening.EstimateConditionText);
        Assert.Equal("可投资", opening.AvailabilityText);
        Assert.True(opening.InvestCommand.CanExecute(null));
    }
}
