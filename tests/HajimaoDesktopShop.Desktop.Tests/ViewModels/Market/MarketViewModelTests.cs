using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;

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
    public void Refresh_PreservesAContinuousPresentationTickAcrossAnimationCycles()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create(), reduceMotion: () => false);

        for (var index = 0; index < 8; index++)
        {
            viewModel.Refresh();
        }

        Assert.Equal(8, viewModel.SceneFrame!.AnimationFrame);
        Assert.Equal(8, viewModel.CommercialStreet.SceneFrame!.AnimationFrame);
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
    public void DesktopNavigation_DoesNotAdvanceSimulationOrChangeCash()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var gameMinute = viewModel.SceneFrame!.Snapshot.GameMinute;
        var cash = viewModel.CashText;

        viewModel.DesktopNavigation.OpenStoreCommand.Execute("corner-store");
        viewModel.DesktopNavigation.BackToStreetCommand.Execute(null);

        Assert.Equal(gameMinute, viewModel.SceneFrame.Snapshot.GameMinute);
        Assert.Equal(cash, viewModel.CashText);
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

    [Fact]
    public void SelectShopObject_ShelfBuildsAggregateDetailAndNavigatesToProducts()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Shelf,
            "ambient"));

        Assert.Equal(ManagementSection.Products, viewModel.SelectedSection);
        Assert.NotNull(viewModel.SelectedShopObject);
        Assert.Equal("常温货架", viewModel.SelectedShopObject.Title);
        Assert.Equal("商品与库存", viewModel.SelectedShopObject.CategoryText);
        Assert.Contains("库存 0/20", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("缺货 1", viewModel.SelectedShopObject.StatusText);
    }

    [Fact]
    public void SelectShopObject_EmployeeBuildsOperationalDetailAndNavigatesToEmployees()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Employee,
            "starter-cashier"));

        Assert.Equal(ManagementSection.Employees, viewModel.SelectedSection);
        Assert.NotNull(viewModel.SelectedShopObject);
        Assert.Equal("小葵", viewModel.SelectedShopObject.Title);
        Assert.Equal("收银员", viewModel.SelectedShopObject.CategoryText);
        Assert.Contains("效率 96%", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("工资 ¥60.00/小时", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("体力 100%", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("满意度 70%", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("班次", viewModel.SelectedShopObject.StatusText);
    }

    [Fact]
    public void Refresh_ReprojectsSelectionWithoutAdvancingGameTime()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Employee,
            "starter-restocker"));
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        viewModel.Refresh();

        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);
        Assert.Equal("阿澄", viewModel.SelectedShopObject!.Title);
    }

    [Fact]
    public void SelectingAnotherStore_ClearsObjectThatIsNotInThatStore()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Employee,
            "starter-cashier"));

        viewModel.SelectStoreCommand.Execute(
            viewModel.Stores.Single(store => store.Id == "station-store"));

        Assert.Null(viewModel.SelectedShopObject);
    }

    [Fact]
    public void QuickRestockSelectedShelf_UsesLocalOrderWithoutAdvancingTime()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session);
        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Shelf,
            "ambient"));
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        Assert.Equal("water", viewModel.SelectedShopObject?.ActionTargetKey);
        Assert.Contains("矿泉水 0/20", viewModel.SelectedShopObject?.ActionHintText);
        Assert.True(viewModel.QuickRestockSelectedShelfCommand.CanExecute(null));
        Assert.False(viewModel.TrainSelectedEmployeeCommand.CanExecute(null));

        viewModel.QuickRestockSelectedShelfCommand.Execute(null);

        var snapshot = viewModel.SceneFrame.Snapshot;
        var water = snapshot.Business.Stores
            .Single(store => store.Id == "corner-store")
            .Products.Single(product => product.Id == "water");
        Assert.Equal(5, water.Quantity);
        Assert.Equal(99_375, snapshot.Business.CashCents);
        Assert.Equal(minute, snapshot.GameMinute);
        Assert.Contains("矿泉水 5/20", viewModel.SelectedShopObject!.ActionHintText);
    }

    [Fact]
    public void ToggleAutoRestockSelectedShelf_UpdatesRealPolicyAndActionText()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session);
        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Shelf,
            "ambient"));

        Assert.Equal("开启自动补货", viewModel.SelectedShelfAutoRestockActionText);

        viewModel.ToggleAutoRestockSelectedShelfCommand.Execute(null);

        Assert.Contains(
            session.Game.GetProcurementSnapshot().AutoRestockPolicies,
            policy => policy.ProductId == "water" && policy.IsEnabled);
        Assert.Equal("关闭自动补货", viewModel.SelectedShelfAutoRestockActionText);

        viewModel.ToggleAutoRestockSelectedShelfCommand.Execute(null);

        Assert.Contains(
            session.Game.GetProcurementSnapshot().AutoRestockPolicies,
            policy => policy.ProductId == "water" && !policy.IsEnabled);
        Assert.Equal("开启自动补货", viewModel.SelectedShelfAutoRestockActionText);
    }

    [Fact]
    public void SelectedEmployeeActions_TrainAndApplyShiftsWithoutAdvancingTime()
    {
        var session = MarketTestSession.Create(openingCashCents: 2_000_000);
        var viewModel = new MarketViewModel(session);
        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Employee,
            "starter-cashier"));
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        Assert.True(viewModel.TrainSelectedEmployeeCommand.CanExecute(null));
        Assert.False(viewModel.QuickRestockSelectedShelfCommand.CanExecute(null));
        Assert.Contains("培训 Lv.0", viewModel.SelectedShopObject?.ActionHintText);

        viewModel.TrainSelectedEmployeeCommand.Execute(null);

        var trained = session.Simulation.Employees.GetSnapshot().Employees
            .Single(employee => employee.EmployeeId == "starter-cashier");
        Assert.Equal(1, trained.TrainingLevel);
        Assert.Contains("培训 Lv.1", viewModel.SelectedShopObject?.ActionHintText);
        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);

        viewModel.SetSelectedEmployeeDayShiftCommand.Execute(null);
        Assert.Contains("班次 08:00–16:00", viewModel.SelectedShopObject?.StatusText);

        viewModel.SetSelectedEmployeeNightShiftCommand.Execute(null);
        Assert.Contains("班次 17:00–01:00", viewModel.SelectedShopObject?.StatusText);
        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);
    }

    private static BusinessShopInteractionTarget Target(
        BusinessShopInteractionKind kind,
        string key) =>
        new(kind, key, new LogicalPixelRect(0, 0, 1, 1));
}
