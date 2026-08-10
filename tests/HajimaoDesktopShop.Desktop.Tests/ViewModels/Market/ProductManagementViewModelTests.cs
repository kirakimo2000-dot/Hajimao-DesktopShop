using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class ProductManagementViewModelTests
{
    [Fact]
    public void LegacyProductCommands_AdjustPricePlaceOrderAndCanDisableDefaultPolicy()
    {
        var session = MarketTestSession.Create(openingCashCents: 1_000_000);
        var page = new ProductManagementViewModel(session, () => "corner-store");
        page.Refresh();
        var water = Assert.Single(page.Products);

        water.IncreasePriceCommand.Execute(null);
        water.OrderRegionalCommand.Execute(null);
        water.ToggleAutoRestockCommand.Execute(null);
        page.Refresh();

        Assert.Equal(210, water.SalePriceCents);
        Assert.Contains(
            page.PendingOrders,
            order => order.ChannelName == "区域配送" && order.Quantity == 6);
        Assert.False(water.IsAutoRestockEnabled);
        Assert.Equal(5, water.ReorderPoint);
        Assert.Equal(16, water.TargetQuantity);
    }

    [Fact]
    public void Refresh_ReusesStableProductItemsAndMapsGrossMargin()
    {
        var page = new ProductManagementViewModel(
            MarketTestSession.Create(openingCashCents: 1_000_000),
            () => "corner-store");
        page.Refresh();
        var water = Assert.Single(page.Products);

        page.Refresh();

        Assert.Same(water, Assert.Single(page.Products));
        Assert.Equal(100, water.UnitGrossProfitCents);
        Assert.Equal("50.0%", water.GrossMarginText);
    }
}
