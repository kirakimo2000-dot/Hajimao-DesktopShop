using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Onboarding;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class OnboardingViewModel : ObservableObject
{
    private string _progressText = string.Empty;
    private string _title = string.Empty;
    private string _guidance = string.Empty;
    private ManagementSection _suggestedSection = ManagementSection.Store;
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
            Guidance = "你已经掌握 Hajimao Market 的核心经营循环。";
            SuggestedSection = ManagementSection.Store;
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
            OnboardingTaskId.RestockProduct => new(
                "第一次进货",
                "为任意商品补充库存，让小店可以持续营业。",
                ManagementSection.Procurement),
            OnboardingTaskId.AdjustPrice => new(
                "调整商品价格",
                "根据毛利和需求调整任意商品售价。",
                ManagementSection.Products),
            OnboardingTaskId.EnableAutoRestock => new(
                "设置自动补货",
                "为常卖商品开启自动补货，让挂机真正持续。",
                ManagementSection.Procurement),
            OnboardingTaskId.CompleteFirstSale => new(
                "完成第一笔销售",
                "保持库存并等待顾客完成结账。",
                ManagementSection.Store),
            OnboardingTaskId.TrainEmployee => new(
                "培训一名员工",
                "培训员工，提高效率并承担相应工资成本。",
                ManagementSection.Employees),
            OnboardingTaskId.UpgradeStore => new(
                "完成一次店铺成长",
                "扩建、升级货架或装修任意一项。",
                ManagementSection.Growth),
            OnboardingTaskId.OpenSecondStore => new(
                "开设第二家店",
                "提升等级并积累资金，在店铺总览开设新店。",
                ManagementSection.Store),
            _ => throw new ArgumentOutOfRangeException(nameof(taskId), taskId, null)
        };

    private readonly record struct OnboardingTaskPresentation(
        string Title,
        string Guidance,
        ManagementSection SuggestedSection);
}
