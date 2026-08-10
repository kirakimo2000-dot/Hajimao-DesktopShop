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
            OnboardingTaskId.ReviewEconomy => new(
                "查看经营概览",
                "先看收入、利润率、现金续航和主要瓶颈。",
                ManagementSection.Overview),
            OnboardingTaskId.ChooseStoreStrategy => new(
                "选择整店策略",
                "尝试高周转、高毛利、精益或充足策略，系统会负责执行。",
                ManagementSection.Strategy),
            OnboardingTaskId.CompleteFirstSale => new(
                "等待第一笔销售",
                "保持游戏运行，观察系统完成进货、服务与结账。",
                ManagementSection.Overview),
            OnboardingTaskId.ReachPositiveDay => new(
                "实现首个盈利日",
                "根据瓶颈调整策略，让完整一天的净利润转正。",
                ManagementSection.Overview),
            OnboardingTaskId.MakeFirstInvestment => new(
                "完成第一次投资",
                "把现金投入扩建、货架或装修，提升长期经营能力。",
                ManagementSection.Investment),
            OnboardingTaskId.OpenSecondStore => new(
                "开设第二家店",
                "提升等级并积累资金，把盈利能力复制到新店。",
                ManagementSection.Investment),
            _ => throw new ArgumentOutOfRangeException(nameof(taskId), taskId, null)
        };

    private readonly record struct OnboardingTaskPresentation(
        string Title,
        string Guidance,
        ManagementSection SuggestedSection);
}
