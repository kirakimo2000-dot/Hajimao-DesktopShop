using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class NextActionViewModelTests
{
    [Fact]
    public void Update_PrioritizesTheCurrentOnboardingAction()
    {
        var onboarding = new OnboardingViewModel();
        onboarding.Refresh(OnboardingSnapshot(completedTasks: 0, OnboardingTaskId.ChooseStoreStrategy));
        var progression = new LongTermProgressionViewModel();
        var nextAction = new NextActionViewModel();

        nextAction.Update(onboarding, progression);

        Assert.Equal("新手任务 1/3", nextAction.ContextText);
        Assert.Equal("选择整店策略", nextAction.Title);
        Assert.Equal("选择高周转、高毛利或稳健备货。", nextAction.DetailText);
        Assert.Equal("选策略", nextAction.ActionText);
        Assert.Equal(ManagementSection.Strategy, nextAction.SuggestedSection);
    }

    [Fact]
    public void Update_FallsThroughToTheLongTermGoalAfterOnboarding()
    {
        var onboarding = new OnboardingViewModel();
        onboarding.Refresh(OnboardingSnapshot(completedTasks: 3, currentTaskId: null));
        var progression = new LongTermProgressionViewModel();
        progression.Update(
            new LongTermProgressionSnapshot(
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
                SharedCashCents: 35_000),
            Catalog());
        var nextAction = new NextActionViewModel();

        nextAction.Update(onboarding, progression);

        Assert.Equal("长期目标", nextAction.ContextText);
        Assert.Equal("为第二家店准备资本", nextAction.Title);
        Assert.Equal("车站便利店 · 现金 ¥350.00/¥800.00", nextAction.DetailText);
        Assert.Equal("看投资", nextAction.ActionText);
        Assert.Equal(ManagementSection.Investment, nextAction.SuggestedSection);
    }

    private static OnboardingSnapshot OnboardingSnapshot(
        int completedTasks,
        OnboardingTaskId? currentTaskId) =>
        new(
            Enum.GetValues<OnboardingTaskId>()
                .Select(id => new OnboardingTaskState(id, (int)id < completedTasks)),
            completedTasks,
            currentTaskId);

    private static StoreCatalogItemSnapshot[] Catalog() =>
    [
        new("corner-store", "街角便利店", 1, 0, IsOpen: true),
        new("station-store", "车站便利店", 3, 80_000, IsOpen: false),
        new("community-store", "社区生活店", 5, 200_000, IsOpen: false)
    ];
}
