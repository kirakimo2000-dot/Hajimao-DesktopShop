using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class OnboardingViewModelTests
{
    [Theory]
    [MemberData(nameof(TaskPresentationCases))]
    public void Refresh_MapsCurrentTaskToChinesePresentation(
        OnboardingTaskId currentTaskId,
        int completedTasks,
        string expectedTitle,
        string expectedGuidance,
        ManagementSection expectedSection)
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(completedTasks, currentTaskId));

        Assert.Equal($"新手任务 {completedTasks + 1}/7", viewModel.ProgressText);
        Assert.Equal(expectedTitle, viewModel.Title);
        Assert.Equal(expectedGuidance, viewModel.Guidance);
        Assert.Equal(expectedSection, viewModel.SuggestedSection);
        Assert.True(viewModel.IsVisible);
    }

    [Fact]
    public void Refresh_WhenAllTasksComplete_HidesCardAndShowsCompletionCopy()
    {
        var viewModel = new OnboardingViewModel();

        viewModel.Refresh(Snapshot(completedTasks: 7, currentTaskId: null));

        Assert.Equal("新手任务已完成", viewModel.ProgressText);
        Assert.Equal("新手任务已完成", viewModel.Title);
        Assert.Equal(ProductIdentity.OnboardingCompletionGuidance, viewModel.Guidance);
        Assert.Equal(ManagementSection.Store, viewModel.SuggestedSection);
        Assert.False(viewModel.IsVisible);
    }

    [Fact]
    public void Refresh_RejectsNullSnapshot()
    {
        var viewModel = new OnboardingViewModel();

        Assert.Throws<ArgumentNullException>(() => viewModel.Refresh(null!));
    }

    [Fact]
    public void Refresh_RaisesPropertyChangesForPresentationProperties()
    {
        var viewModel = new OnboardingViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.Refresh(Snapshot(completedTasks: 1, OnboardingTaskId.AdjustPrice));

        Assert.Contains(nameof(OnboardingViewModel.ProgressText), changed);
        Assert.Contains(nameof(OnboardingViewModel.Title), changed);
        Assert.Contains(nameof(OnboardingViewModel.Guidance), changed);
        Assert.Contains(nameof(OnboardingViewModel.SuggestedSection), changed);
        Assert.Contains(nameof(OnboardingViewModel.IsVisible), changed);
    }

    public static TheoryData<OnboardingTaskId, int, string, string, ManagementSection> TaskPresentationCases() =>
        new()
        {
            {
                OnboardingTaskId.RestockProduct,
                0,
                "第一次进货",
                "为任意商品补充库存，让小店可以持续营业。",
                ManagementSection.Procurement
            },
            {
                OnboardingTaskId.AdjustPrice,
                1,
                "调整商品价格",
                "根据毛利和需求调整任意商品售价。",
                ManagementSection.Products
            },
            {
                OnboardingTaskId.EnableAutoRestock,
                2,
                "设置自动补货",
                "为常卖商品开启自动补货，让挂机真正持续。",
                ManagementSection.Procurement
            },
            {
                OnboardingTaskId.CompleteFirstSale,
                3,
                "完成第一笔销售",
                "保持库存并等待顾客完成结账。",
                ManagementSection.Store
            },
            {
                OnboardingTaskId.TrainEmployee,
                4,
                "培训一名员工",
                "培训员工，提高效率并承担相应工资成本。",
                ManagementSection.Employees
            },
            {
                OnboardingTaskId.UpgradeStore,
                5,
                "完成一次店铺成长",
                "扩建、升级货架或装修任意一项。",
                ManagementSection.Growth
            },
            {
                OnboardingTaskId.OpenSecondStore,
                6,
                "开设第二家店",
                "提升等级并积累资金，在店铺总览开设新店。",
                ManagementSection.Store
            }
        };

    private static OnboardingSnapshot Snapshot(int completedTasks, OnboardingTaskId? currentTaskId) =>
        new(
            Enum.GetValues<OnboardingTaskId>()
                .Select(id => new OnboardingTaskState(id, IsCompleted: (int)id < completedTasks)),
            completedTasks,
            currentTaskId);
}
