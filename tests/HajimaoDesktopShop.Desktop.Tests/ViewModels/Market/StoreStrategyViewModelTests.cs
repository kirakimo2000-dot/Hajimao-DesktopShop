using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreStrategyViewModelTests
{
    [Fact]
    public void PricingCommand_AppliesWholeStoreAndRetainsStockingChoice()
    {
        var session = MarketTestSession.Create();
        var viewModel = new StoreStrategyViewModel(session, () => "corner-store");

        viewModel.UseFullShelvesStockingCommand.Execute(null);
        viewModel.UseHighMarginPricingCommand.Execute(null);

        var applied = session.Strategy.GetAppliedPlan("corner-store");
        Assert.NotNull(applied);
        Assert.Equal(StorePricingPreset.HighMargin, applied.Pricing);
        Assert.Equal(StoreStockingPreset.FullShelves, applied.Stocking);
        Assert.Equal("高毛利", viewModel.CurrentPricingText);
        Assert.Equal("充足货架", viewModel.CurrentStockingText);
        Assert.True(viewModel.IsHighMarginPricing);
        Assert.False(viewModel.IsBalancedPricing);
        Assert.True(viewModel.IsFullShelvesStocking);
        Assert.False(viewModel.IsBalancedStocking);
        Assert.Single(viewModel.Products);
    }

    [Fact]
    public void StockingCommand_AppliesWholeStoreAndRetainsPricingChoice()
    {
        var session = MarketTestSession.Create();
        var viewModel = new StoreStrategyViewModel(session, () => "corner-store");

        viewModel.UseHighTurnoverPricingCommand.Execute(null);
        viewModel.UseLeanStockingCommand.Execute(null);

        var applied = session.Strategy.GetAppliedPlan("corner-store");
        Assert.NotNull(applied);
        Assert.Equal(StorePricingPreset.HighTurnover, applied.Pricing);
        Assert.Equal(StoreStockingPreset.Lean, applied.Stocking);
        Assert.Contains("整店策略已应用", viewModel.StatusMessage);
    }

    [Fact]
    public void RecoveryCommand_AppearsOnlyForFinancialDangerAndUsesExistingStrategy()
    {
        var session = MarketTestSession.Create();
        session.Simulation.AdvanceRealSeconds(1_440);
        var cashBefore = session.Game.GetSnapshot().CashCents;
        var viewModel = new StoreStrategyViewModel(session, () => "corner-store");

        Assert.True(viewModel.HasRecoveryRecommendation);
        Assert.Contains("亏损", viewModel.RecoveryGuidanceText);
        Assert.True(viewModel.ApplyRecoveryCommand.CanExecute(null));

        viewModel.ApplyRecoveryCommand.Execute(null);

        Assert.Equal(cashBefore, session.Game.GetSnapshot().CashCents);
        Assert.Equal(StoreStockingPreset.Lean, session.Strategy.GetAppliedPlan("corner-store")?.Stocking);
        Assert.Contains("保守方案已应用", viewModel.StatusMessage);
    }
}
