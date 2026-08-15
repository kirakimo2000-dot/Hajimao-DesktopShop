using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreEconomyViewModelTests
{
    [Fact]
    public void Update_ExplainsActualCombatResultsAndCurrentLoadout()
    {
        var store = new StoreCombatSnapshot(
            "corner-store",
            StoreCombatState.Empty,
            [],
            [],
            12_500,
            18,
            2,
            4);
        var loadout = new StoreProductLoadout("corner-store", 3, ["water", "chips"]);
        var viewModel = new StoreEconomyViewModel();

        viewModel.Update(store, loadout, unlockedProducts: 7);

        Assert.Equal("开店以来", viewModel.PeriodText);
        Assert.Equal("累计招待 18 位 · 收入 ¥125.00", viewModel.PerformanceHeadlineText);
        Assert.Contains("掉落 4 件", viewModel.PerformanceDetailText, StringComparison.Ordinal);
        Assert.Contains("装备 2/3", viewModel.ReasonDetailText, StringComparison.Ordinal);
        Assert.Contains("已发现商品 7", viewModel.ReasonDetailText, StringComparison.Ordinal);
        Assert.DoesNotContain("库存", viewModel.PerformanceDetailText, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_ShowsOnePlainLanguageTraitForCurrentCustomer()
    {
        var customer = new ActiveCustomerState(
            1,
            "delivery-rider",
            80,
            4_000,
            96,
            ["delivery", "commuter", "evening"],
            new Dictionary<string, int> { ["liquid"] = 160 },
            0,
            0,
            155);
        var store = new StoreCombatSnapshot(
            "corner-store",
            new StoreCombatState(2, 0, 0, [customer], []),
            [], [], 0, 0, 0, 0);
        var viewModel = new StoreEconomyViewModel();

        viewModel.Update(store, new StoreProductLoadout("corner-store", 3, ["water"]), 3);

        Assert.Contains("外卖骑手", viewModel.ReasonHeadlineText, StringComparison.Ordinal);
        Assert.Contains("移动较快", viewModel.ReasonDetailText, StringComparison.Ordinal);
        Assert.Contains("液体类效果较弱", viewModel.ReasonDetailText, StringComparison.Ordinal);
    }
}
