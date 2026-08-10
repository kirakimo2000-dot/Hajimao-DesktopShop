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

        Assert.Equal($"新手任务 {completedTasks + 1}/4", viewModel.ProgressText);
        Assert.Equal(expectedTitle, viewModel.Title);
        Assert.Equal(expectedGuidance, viewModel.Guidance);
        Assert.Equal(expectedSection, viewModel.SuggestedSection);
        Assert.True(viewModel.IsVisible);
    }

    [Fact]
    public void Refresh_WhenAllTasksComplete_HidesCard()
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(4, null));

        Assert.Equal(ManagementSection.Overview, viewModel.SuggestedSection);
        Assert.False(viewModel.IsVisible);
    }

    public static TheoryData<OnboardingTaskId, int, string, string, ManagementSection> TaskPresentationCases() =>
        new()
        {
            { OnboardingTaskId.ReviewEconomy, 0, "查看经营概览", "先看收入、利润率、现金续航和主要瓶颈。", ManagementSection.Overview },
            { OnboardingTaskId.ChooseStoreStrategy, 1, "选择整店策略", "尝试高周转、高毛利、精益或充足策略，系统会负责执行。", ManagementSection.Strategy },
            { OnboardingTaskId.MakeFirstInvestment, 2, "完成第一次投资", "选择一个能改变现金流或经营能力的方案，不需要逐项维护。", ManagementSection.Investment },
            { OnboardingTaskId.ReviewInvestmentReturn, 3, "查看投资回报", "等待下一份完整日结，对比净利润、成交与流失变化。", ManagementSection.Investment }
        };

    private static OnboardingSnapshot Snapshot(int completedTasks, OnboardingTaskId? currentTaskId) =>
        new(
            Enum.GetValues<OnboardingTaskId>()
                .Select(id => new OnboardingTaskState(id, (int)id < completedTasks)),
            completedTasks,
            currentTaskId);
}
