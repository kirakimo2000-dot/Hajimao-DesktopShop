using HajimaoDesktopShop.Application.Business.Events;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class MarketEventTickerViewModelTests
{
    [Fact]
    public void Update_ShowsOneNaturalLanguageEventWithoutExposingCoefficients()
    {
        var viewModel = new MarketEventTickerViewModel();
        var snapshot = CreateSnapshot(
            new ActiveMarketEventSnapshot(
                "morning-commute",
                "早高峰提前",
                "通勤客流上升，排队压力增大。",
                119,
                [new MarketEventEffect(MarketEventEffectKind.Traffic, 180)],
                []));

        viewModel.Update(snapshot);

        Assert.True(viewModel.IsVisible);
        Assert.Equal("早高峰提前 · 通勤客流上升，排队压力增大。（剩余 2 小时）", viewModel.Text);
        Assert.DoesNotContain("180", viewModel.Text, StringComparison.Ordinal);
        Assert.DoesNotContain('%', viewModel.Text);
    }

    [Fact]
    public void Update_WhenSeveralEventsAreActive_ShowsTheSoonestEndingEvent()
    {
        var viewModel = new MarketEventTickerViewModel();
        var later = CreateEvent("天气晴好", "到店客增加。", 240);
        var sooner = CreateEvent("午餐高峰", "短时进店人数增加。", 45);

        viewModel.Update(CreateSnapshot(later, sooner));

        Assert.Equal("午餐高峰 · 短时进店人数增加。（剩余 45 分钟）", viewModel.Text);
    }

    [Fact]
    public void Update_WhenNoEventIsActive_HidesTicker()
    {
        var viewModel = new MarketEventTickerViewModel();

        viewModel.Update(CreateSnapshot());

        Assert.False(viewModel.IsVisible);
        Assert.Equal(string.Empty, viewModel.Text);
    }

    private static ActiveMarketEventSnapshot CreateEvent(
        string headline,
        string summary,
        int remainingMinutes) =>
        new(
            headline,
            headline,
            summary,
            remainingMinutes,
            [new MarketEventEffect(MarketEventEffectKind.PurchaseChance, 100)],
            []);

    private static MarketEventSchedulerSnapshot CreateSnapshot(
        params ActiveMarketEventSnapshot[] events) =>
        new(0, 1, 120, events, new Dictionary<string, int>());
}
