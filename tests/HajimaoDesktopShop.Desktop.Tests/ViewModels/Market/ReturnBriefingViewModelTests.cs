using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class ReturnBriefingViewModelTests
{
    [Fact]
    public void MarketViewModel_FormatsOfflineResultAsAReadOnlyDecisionBriefing()
    {
        var session = MarketTestSession.Create();
        var settlement = new OfflineSettlementResult(
            RequestedSeconds: 1_440,
            AppliedSeconds: 1_440,
            WasCapped: false,
            OfflineTimeAnomaly.None,
            new OfflineBusinessTotals(100_000, 0, 0, 0, 0, 0),
            new OfflineBusinessTotals(99_000, 2_000, 1_000, 500, -1_000, 10),
            new BusinessDayReport(
                1,
                [new StoreDayReport(
                    "corner-store", 20, 10, 10, 2, 2_000, 1_000, 1_000, -1_000, 800, 0)]));

        var viewModel = new MarketViewModel(session, offlineSettlement: settlement);

        Assert.True(viewModel.ReturnBriefing.IsVisible);
        Assert.Contains("离线 1 个经营日", viewModel.ReturnBriefing.DurationText);
        Assert.Contains("现金 -¥10.00", viewModel.ReturnBriefing.ResultText);
        Assert.Contains("成交 +10", viewModel.ReturnBriefing.ResultText);
        Assert.Contains("关注街角便利店", viewModel.ReturnBriefing.GuidanceText);
    }

    [Fact]
    public void MarketViewModel_HidesBriefingWithoutAnOfflineSettlement()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        Assert.False(viewModel.ReturnBriefing.IsVisible);
    }
}
