using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreGrowthManagementViewModelTests
{
    [Fact]
    public void GrowthPage_UpgradesAndStartsPromotionThroughApplicationCommands()
    {
        var session = MarketTestSession.Create(openingCashCents: 2_000_000);
        var page = new StoreGrowthManagementViewModel(session, () => "corner-store");

        page.UpgradeShelfCommand.Execute(null);
        page.StartFlyersCommand.Execute(null);
        page.Refresh();

        Assert.Equal(1, page.ShelfLevel);
        Assert.Equal("本地传单 · 剩余 240 分钟", page.ActivePromotionText);
        Assert.Equal("125%", page.InventoryCapacityText);
    }
}
