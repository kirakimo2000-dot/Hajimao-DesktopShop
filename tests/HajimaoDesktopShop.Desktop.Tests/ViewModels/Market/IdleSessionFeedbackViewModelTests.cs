using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class IdleSessionFeedbackViewModelTests
{
    [Fact]
    public void NewSession_ExplainsAutomaticCombatWithoutAClockOrReportTimer()
    {
        var starting = Snapshot(revenue: 2_000, served: 8, escaped: 2, drops: 3, active: 1);
        var viewModel = new IdleSessionFeedbackViewModel(starting);

        viewModel.Update(starting);

        Assert.Equal("本次 +¥0.00", viewModel.SessionProfitShortText);
        Assert.Equal("本次招待 0 位顾客", viewModel.SessionCustomerText);
        Assert.Equal("本次掉落 0 件商品", viewModel.SessionDropText);
        Assert.Equal("Neutral", viewModel.ProfitTone);
        Assert.Equal("毛毛正在自动招待顾客", viewModel.GoalText);
        Assert.Contains("店内顾客 1", viewModel.RecentActivityText, StringComparison.Ordinal);
    }

    [Fact]
    public void RunningSession_ProjectsOnlyRevenueServiceEscapeAndDrops()
    {
        var viewModel = new IdleSessionFeedbackViewModel(
            Snapshot(revenue: 2_000, served: 8, escaped: 2, drops: 3));

        viewModel.Update(Snapshot(revenue: 3_250, served: 11, escaped: 3, drops: 5));

        Assert.Equal("本次挂机收入 +¥12.50", viewModel.SessionProfitText);
        Assert.Equal("本次 +¥12.50", viewModel.SessionProfitShortText);
        Assert.Equal("本次招待 3 位顾客", viewModel.SessionCustomerText);
        Assert.Equal("本次掉落 2 件商品", viewModel.SessionDropText);
        Assert.Equal("漏掉 1 位", viewModel.EscapedCustomerText);
        Assert.Equal("Positive", viewModel.ProfitTone);
        Assert.Contains("刚刚完成 3 次招待", viewModel.RecentActivityText, StringComparison.Ordinal);
        Assert.Equal("去战斗策略调整商品组合", viewModel.GoalText);
    }

    [Fact]
    public void MarketViewModel_UsesCombatSnapshotInsteadOfLegacyRetailSimulation()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session);

        session.Simulation.AdvanceRealSeconds(360);
        viewModel.Refresh();

        Assert.Equal("本次招待 0 位顾客", viewModel.IdleFeedback.SessionCustomerText);
        Assert.Equal("本次掉落 0 件商品", viewModel.IdleFeedback.SessionDropText);
        Assert.DoesNotContain("成交", viewModel.IdleFeedback.RecentActivityText, StringComparison.Ordinal);
        Assert.DoesNotContain("报告", viewModel.IdleFeedback.GoalText, StringComparison.Ordinal);
    }

    private static BusinessCombatSnapshot Snapshot(
        long revenue,
        int served,
        int escaped,
        int drops,
        int active = 0)
    {
        var customers = Enumerable.Range(0, active)
            .Select(index => new ActiveCustomerState(
                index + 1,
                "regular",
                10,
                5_000,
                100,
                [],
                new Dictionary<string, int>(),
                0,
                0))
            .ToArray();
        var state = StoreCombatState.Empty with { Customers = customers };
        return new BusinessCombatSnapshot(
            10_000,
            [new StoreCombatSnapshot("corner-store", state, [], [], revenue, served, escaped, drops)],
            [],
            []);
    }
}
