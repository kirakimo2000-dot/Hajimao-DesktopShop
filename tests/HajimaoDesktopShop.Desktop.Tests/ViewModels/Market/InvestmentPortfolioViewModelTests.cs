using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class InvestmentPortfolioViewModelTests
{
    [Fact]
    public void NewSession_OnlyOffersUpToThreeNewStoreChoicesWithoutReportGate()
    {
        var viewModel = Create(MarketTestSession.Create());

        Assert.InRange(viewModel.Candidates.Count, 1, 3);
        Assert.All(viewModel.Candidates, candidate =>
        {
            Assert.StartsWith("store:open:store-0002:", candidate.Id, StringComparison.Ordinal);
            Assert.Equal("扩张街区", candidate.ThesisText);
            Assert.DoesNotContain("报告", candidate.ExpectedBenefitText, StringComparison.Ordinal);
            Assert.DoesNotContain("员工", candidate.EffectText, StringComparison.Ordinal);
            Assert.DoesNotContain("货架", candidate.EffectText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AffordableStoreChoice_CanBeOpenedImmediatelyFromActiveIdleIncome()
    {
        var session = MarketTestSession.Create();
        var refreshCount = 0;
        var viewModel = new InvestmentPortfolioViewModel(
            session,
            () => "corner-store",
            () => refreshCount++);
        var candidate = viewModel.Candidates.Single(item => item.TitleText == "开设 FamilyMart");

        candidate.InvestCommand.Execute(null);

        Assert.Equal(1, refreshCount);
        Assert.True(session.Game.IsStoreOpen("store-0002"));
        Assert.Contains("开店成功", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void StoreChoice_ExplainsProfitStyleAndCashRiskInPlainLanguage()
    {
        var viewModel = Create(MarketTestSession.Create());
        var familyMart = viewModel.Candidates.Single(item => item.TitleText == "开设 FamilyMart");

        Assert.Equal("开设 FamilyMart", familyMart.TitleText);
        Assert.Contains("投入", familyMart.CostText, StringComparison.Ordinal);
        Assert.Contains("收益", familyMart.ExpectedBenefitText, StringComparison.Ordinal);
        Assert.Contains("现金", familyMart.CashPressureText, StringComparison.Ordinal);
        Assert.Contains("独立战斗店铺", familyMart.EffectText, StringComparison.Ordinal);
        Assert.Equal("可开店", familyMart.AvailabilityText);
        Assert.True(familyMart.InvestCommand.CanExecute(null));
    }

    [Fact]
    public void UnaffordableStoreChoice_RemainsVisibleButDisabled()
    {
        var viewModel = Create(MarketTestSession.Create(openingCashCents: 10_000));
        Assert.NotEmpty(viewModel.Candidates);
        Assert.All(viewModel.Candidates, opening =>
        {
            Assert.Equal("资金不足", opening.AvailabilityText);
            Assert.False(opening.InvestCommand.CanExecute(null));
        });
    }

    private static InvestmentPortfolioViewModel Create(
        HajimaoDesktopShop.Application.Business.BusinessSession session) =>
        new(session, () => "corner-store", refreshMarket: () => { });
}
