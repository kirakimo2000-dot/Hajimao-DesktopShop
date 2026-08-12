using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Onboarding;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class OnboardingViewModel : ObservableObject
{
    private string _progressText = string.Empty;
    private string _title = string.Empty;
    private string _guidance = string.Empty;
    private ManagementSection _suggestedSection = ManagementSection.Overview;
    private bool _isVisible;

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Guidance
    {
        get => _guidance;
        private set => SetProperty(ref _guidance, value);
    }

    public ManagementSection SuggestedSection
    {
        get => _suggestedSection;
        private set => SetProperty(ref _suggestedSection, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public void Refresh(OnboardingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.IsComplete)
        {
            ProgressText = "新手任务已完成";
            Title = "新手任务已完成";
            Guidance = ProductIdentity.OnboardingCompletionGuidance;
            SuggestedSection = ManagementSection.Overview;
            IsVisible = false;
            return;
        }

        var presentation = GetPresentation(snapshot.CurrentTaskId!.Value);
        ProgressText = $"新手任务 {snapshot.CompletedTasks + 1}/{snapshot.TotalTasks}";
        Title = presentation.Title;
        Guidance = presentation.Guidance;
        SuggestedSection = presentation.SuggestedSection;
        IsVisible = true;
    }

    private static OnboardingTaskPresentation GetPresentation(OnboardingTaskId taskId) =>
        taskId switch
        {
            OnboardingTaskId.ChooseStoreStrategy => new(
                "选择整店策略",
                "选择高周转、高毛利或稳健备货。",
                ManagementSection.Strategy),
            OnboardingTaskId.MakeFirstInvestment => new(
                "完成第一次投资",
                "比较回报与现金压力，执行一项投资。",
                ManagementSection.Investment),
            OnboardingTaskId.ReviewInvestmentReturn => new(
                "查看投资回报",
                "等待下一次完整日结，查看投资前后变化。",
                ManagementSection.Investment),
            _ => throw new ArgumentOutOfRangeException(nameof(taskId), taskId, null)
        };

    private readonly record struct OnboardingTaskPresentation(
        string Title,
        string Guidance,
        ManagementSection SuggestedSection);
}
