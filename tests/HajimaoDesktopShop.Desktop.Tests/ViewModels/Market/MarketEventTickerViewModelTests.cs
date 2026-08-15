using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class MarketEventTickerViewModelTests
{
    [Fact]
    public void Update_ShowsCombatCustomerPoolEventInPlainLanguage()
    {
        var viewModel = new MarketEventTickerViewModel();

        viewModel.Update(["morning-commute"]);

        Assert.True(viewModel.IsVisible);
        Assert.Equal("通勤高峰 · 上班族顾客更常出现", viewModel.Text);
        Assert.DoesNotContain('%', viewModel.Text);
    }

    [Fact]
    public void Update_WhenNoEventIsActive_HidesTicker()
    {
        var viewModel = new MarketEventTickerViewModel();

        viewModel.Update([]);

        Assert.False(viewModel.IsVisible);
        Assert.Equal(string.Empty, viewModel.Text);
    }
}
