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

        Assert.Equal($"新手任务 {completedTasks + 1}/3", viewModel.ProgressText);
        Assert.Equal(expectedTitle, viewModel.Title);
        Assert.Equal(expectedGuidance, viewModel.Guidance);
        Assert.Equal(expectedSection, viewModel.SuggestedSection);
        Assert.True(viewModel.IsVisible);
    }

    [Fact]
    public void Refresh_WhenAllTasksComplete_HidesCard()
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(3, null));

        Assert.Equal(ManagementSection.Overview, viewModel.SuggestedSection);
        Assert.False(viewModel.IsVisible);
    }

    public static TheoryData<OnboardingTaskId, int, string, string, ManagementSection> TaskPresentationCases() =>
        new()
        {
            { OnboardingTaskId.ChooseStoreStrategy, 0, "选择整店策略", "选择高周转、高毛利或稳健备货。", ManagementSection.Strategy },
            { OnboardingTaskId.MakeFirstInvestment, 1, "完成第一次投资", "比较回报与现金压力，执行一项投资。", ManagementSection.Investment },
            { OnboardingTaskId.ReviewInvestmentReturn, 2, "查看投资回报", "等待下一次完整日结，查看投资前后变化。", ManagementSection.Investment }
        };

    private static OnboardingSnapshot Snapshot(int completedTasks, OnboardingTaskId? currentTaskId) =>
        new(
            Enum.GetValues<OnboardingTaskId>()
                .Select(id => new OnboardingTaskState(id, (int)id < completedTasks)),
            completedTasks,
            currentTaskId);
}
