using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class MarketViewModelTests
{
    [Fact]
    public void Refresh_AdvancesPresentationFrameWithoutChangingGameTime()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session, reduceMotion: () => false);
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        viewModel.Refresh();

        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);
        Assert.Equal(1, viewModel.SceneFrame.AnimationFrame);
        Assert.False(viewModel.SceneFrame.ReduceMotion);
        Assert.Equal(1, viewModel.CommercialStreet.SceneFrame!.AnimationFrame);
    }

    [Fact]
    public void Refresh_ReducedMotionLocksTheSeedFrame()
    {
        var viewModel = new MarketViewModel(
            MarketTestSession.Create(),
            reduceMotion: () => true);

        viewModel.Refresh();

        Assert.Equal(0, viewModel.SceneFrame!.AnimationFrame);
        Assert.True(viewModel.SceneFrame.ReduceMotion);
    }

    [Fact]
    public void Refresh_MapsBusinessStatusAndSelectedStoreWithoutOwningRules()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session);

        viewModel.Refresh();

        Assert.Equal("¥1,000.00", viewModel.CashText);
        Assert.Equal("Lv.1", viewModel.PlayerLevelText);
        Assert.Equal("corner-store", viewModel.SelectedStoreId);
        Assert.Equal("街角便利店", viewModel.SelectedStoreName);
        Assert.Equal("固定现实 1x", viewModel.TimeModeText);
        Assert.Equal("第一次进货", viewModel.Onboarding.Title);
    }

    [Fact]
    public void Navigation_ChangesOneContentSurfaceAndKeepsSceneVisible()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.NavigateCommand.Execute(ManagementSection.Employees);

        Assert.Equal(ManagementSection.Employees, viewModel.SelectedSection);
        Assert.True(viewModel.IsEmployeesSection);
        Assert.False(viewModel.IsProductsSection);
        Assert.NotNull(viewModel.SceneFrame);
    }

    [Fact]
    public void GoToOnboardingTask_NavigatesToSuggestedSectionWithoutChangingGameTime()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        viewModel.GoToOnboardingTaskCommand.Execute(null);

        Assert.Equal(ManagementSection.Procurement, viewModel.SelectedSection);
        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);
    }

    [Fact]
    public void SelectStore_ChangesTheSharedStoreContext()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var station = viewModel.Stores.Single(store => store.Id == "station-store");

        viewModel.SelectStoreCommand.Execute(station);

        Assert.Equal("station-store", viewModel.SelectedStoreId);
        Assert.Equal("车站便利店", viewModel.SelectedStoreName);
        Assert.False(station.IsOpen);
    }

    [Fact]
    public void ToggleStatusBar_ChangesPresentationHeightWithoutChangingGameTime()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        viewModel.ToggleStatusBarCommand.Execute(null);

        Assert.False(viewModel.IsStatusBarExpanded);
        Assert.Equal(34d, viewModel.StatusBarHeight);
        Assert.Equal("展开状态栏", viewModel.StatusBarToggleText);
        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);

        viewModel.ToggleStatusBarCommand.Execute(null);

        Assert.True(viewModel.IsStatusBarExpanded);
        Assert.Equal(56d, viewModel.StatusBarHeight);
        Assert.Equal("收起状态栏", viewModel.StatusBarToggleText);
    }
}
