using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class OnboardingViewModelTests
{
    [Theory]
    [MemberData(nameof(TaskPresentationCases))]
    public void Refresh_MapsInvestorTaskToChinesePresentation(
        OnboardingTaskId taskId,
        int completedTasks,
        string expectedTitle,
        string expectedGuidance,
        ManagementSection expectedSection)
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(completedTasks, taskId));

        Assert.Equal($"新手任务 {completedTasks + 1}/6", viewModel.ProgressText);
        Assert.Equal(expectedTitle, viewModel.Title);
        Assert.Equal(expectedGuidance, viewModel.Guidance);
        Assert.Equal(expectedSection, viewModel.SuggestedSection);
        Assert.True(viewModel.IsVisible);
    }

    [Fact]
    public void Refresh_WhenAllTasksComplete_HidesCard()
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(6, null));

        Assert.Equal(ManagementSection.Overview, viewModel.SuggestedSection);
        Assert.False(viewModel.IsVisible);
    }

    public static TheoryData<OnboardingTaskId, int, string, string, ManagementSection> TaskPresentationCases() =>
        new()
        {
            { OnboardingTaskId.ReviewEconomy, 0, "查看经营概览", "先看收入、利润率、现金续航和主要瓶颈。", ManagementSection.Overview },
            { OnboardingTaskId.ChooseStoreStrategy, 1, "选择整店策略", "在定价和备货中各选一项，系统会负责执行。", ManagementSection.Strategy },
            { OnboardingTaskId.CompleteFirstSale, 2, "等待第一笔销售", "保持游戏运行，观察系统完成进货、服务与结账。", ManagementSection.Overview },
            { OnboardingTaskId.ReachPositiveDay, 3, "实现首个盈利日", "根据瓶颈调整策略，让完整一天的净利润转正。", ManagementSection.Overview },
            { OnboardingTaskId.MakeFirstInvestment, 4, "完成第一次投资", "把现金投入扩建、货架、装修或关键员工。", ManagementSection.Investment },
            { OnboardingTaskId.OpenSecondStore, 5, "开设第二家店", "提升等级并积累资金，把盈利能力复制到新店。", ManagementSection.Investment }
        };

    private static OnboardingSnapshot Snapshot(int completedTasks, OnboardingTaskId? currentTaskId) =>
        new(
            Enum.GetValues<OnboardingTaskId>()
                .Select(id => new OnboardingTaskState(id, (int)id < completedTasks)),
            completedTasks,
            currentTaskId);
}
