using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class LongTermProgressionViewModelTests
{
    [Fact]
    public void Update_FormatsCapitalPreparationWithoutPromisingWallClockCompletion()
    {
        var viewModel = new LongTermProgressionViewModel();
        var snapshot = new LongTermProgressionSnapshot(
            new ProgressionGoalSnapshot(
                ProgressionGoalId.PrepareSecondStore,
                "station-store",
                CurrentValue: 35_000,
                TargetValue: 80_000,
                RequiredPlayerLevel: 0,
                RequiredCashCents: 80_000),
            OpenStoreCount: 1,
            ConfiguredStoreCount: 3,
            PlayerLevel: 2,
            SharedCashCents: 35_000);

        viewModel.Update(snapshot, Catalog());

        Assert.Equal("为第二家店准备资本", viewModel.TitleText);
        Assert.Equal("车站便利店 · 现金 ¥350.00/¥800.00", viewModel.ProgressText);
        Assert.Contains("比较当前店铺投资与开店储备", viewModel.GuidanceText);
        Assert.DoesNotContain("分钟", viewModel.GuidanceText);
        Assert.DoesNotContain("小时", viewModel.GuidanceText);
    }

    [Fact]
    public void Update_FormatsWeakestStoreAndCommercialBlockAsPersistentGoals()
    {
        var viewModel = new LongTermProgressionViewModel();
        var strengthening = new LongTermProgressionSnapshot(
            new ProgressionGoalSnapshot(
                ProgressionGoalId.StrengthenPortfolio,
                "station-store",
                CurrentValue: 1,
                TargetValue: 2),
            2,
            3,
            5,
            100_000);

        viewModel.Update(strengthening, Catalog());
        Assert.Equal("强化最弱店铺", viewModel.TitleText);
        Assert.Equal("车站便利店 · 成长 1/2", viewModel.ProgressText);

        viewModel.Update(
            strengthening with
            {
                CurrentGoal = new ProgressionGoalSnapshot(
                    ProgressionGoalId.UnlockCommercialBlock,
                    string.Empty,
                    CurrentValue: 8,
                    TargetValue: 10,
                    RequiredPlayerLevel: 10),
                OpenStoreCount = 3,
                PlayerLevel = 8
            },
            Catalog(openStores: 3));
        Assert.Equal("解锁完整街区", viewModel.TitleText);
        Assert.Equal("完整街区 Lv.8/10", viewModel.ProgressText);
    }

    private static StoreCatalogItemSnapshot[] Catalog(int openStores = 1) =>
    [
        new("corner-store", "街角便利店", 1, 0, IsOpen: true),
        new("station-store", "车站便利店", 3, 80_000, IsOpen: openStores >= 2),
        new("community-store", "社区生活店", 5, 200_000, IsOpen: openStores >= 3)
    ];
}
