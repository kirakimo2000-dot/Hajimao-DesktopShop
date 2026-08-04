using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class FinanceViewModelTests
{
    [Fact]
    public void FinancePage_SeparatesGrossWagesOperatingCostAndNetProfit()
    {
        var session = MarketTestSession.Create(openingCashCents: 2_000_000);
        session.Game.UpgradeStore("corner-store", StoreUpgradeKind.Shelf);
        var page = new FinanceViewModel(session, () => "corner-store");

        page.Refresh();

        Assert.Equal("¥0.00", page.GrossProfitText);
        Assert.Equal("¥250.00", page.OperatingCostText);
        Assert.Equal("-¥250.00", page.NetProfitText);
    }
}
