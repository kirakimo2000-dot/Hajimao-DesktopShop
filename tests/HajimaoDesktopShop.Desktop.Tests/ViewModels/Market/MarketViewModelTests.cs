using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class MarketViewModelTests
{
    [Fact]
    public void ManagementSection_ContainsOnlyThreeInvestorSurfaces()
    {
        Assert.Equal(
            [ManagementSection.Overview, ManagementSection.Strategy, ManagementSection.Investment],
            Enum.GetValues<ManagementSection>());
    }

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
        Assert.Equal("7-Eleven", viewModel.SelectedStoreName);
        Assert.Equal("选择整店策略", viewModel.Onboarding.Title);
        Assert.NotEmpty(viewModel.Investment.Candidates);
    }

    [Fact]
    public void RecommendedInvestment_CompletesInvestorTaskThroughRecordedEvidence()
    {
        var session = MarketTestSession.Create(openingCashCents: 500_000);
        var viewModel = new MarketViewModel(session);
        var recommendation = viewModel.Investment.Candidates.First(candidate =>
            candidate.InvestCommand.CanExecute(null));

        recommendation.InvestCommand.Execute(null);
        var onboarding = OnboardingProgressService.CreateSnapshot(
            session.Simulation.GetSnapshot(),
            session.Game.GetProcurementSnapshot(),
            session.Investments.HasAnyInvestment);

        Assert.True(onboarding.Tasks.Single(task =>
            task.Id == OnboardingTaskId.MakeFirstInvestment).IsCompleted);
    }

    [Fact]
    public void Refresh_HidesOnboardingAfterALaterDayCanCompareTheInvestment()
    {
        var session = MarketTestSession.Create(openingCashCents: 500_000);
        Assert.Equal(
            StoreStrategyCommandStatus.Success,
            session.Strategy.Apply(
                "corner-store",
                StorePricingPreset.HighMargin,
                StoreStockingPreset.Lean).Status);
        session.Simulation.AdvanceRealSeconds(1_440);
        var shelf = session.Investments.GetPortfolio("corner-store")!.Candidates
            .Single(candidate => candidate.Kind == InvestmentKind.Shelf);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute("corner-store", shelf.Id).Status);
        session.Simulation.AdvanceRealSeconds(1_440);

        var viewModel = new MarketViewModel(session);

        Assert.Equal(
            InvestmentComparisonStatus.Compared,
            session.Investments.GetLatestComparison("corner-store")?.Status);
        Assert.False(viewModel.Onboarding.IsVisible);
    }

    [Fact]
    public void Navigation_ChangesOneContentSurfaceAndKeepsSceneVisible()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.NavigateCommand.Execute(ManagementSection.Strategy);

        Assert.Equal(ManagementSection.Strategy, viewModel.SelectedSection);
        Assert.True(viewModel.IsStrategySection);
        Assert.False(viewModel.IsOverviewSection);
        Assert.NotNull(viewModel.SceneFrame);
    }

    [Fact]
    public void GoToNextAction_NavigatesToSuggestedSectionWithoutChangingGameTime()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var minute = viewModel.SceneFrame!.Snapshot.GameMinute;

        viewModel.GoToNextActionCommand.Execute(null);

        Assert.Equal(ManagementSection.Strategy, viewModel.SelectedSection);
        Assert.Equal(minute, viewModel.SceneFrame.Snapshot.GameMinute);
    }

    [Fact]
    public void SelectStore_ChangesTheSharedStoreContext()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var station = viewModel.Stores.Single(store => store.Id == "station-store");

        viewModel.SelectStoreCommand.Execute(station);

        Assert.Equal("station-store", viewModel.SelectedStoreId);
        Assert.Equal("FamilyMart", viewModel.SelectedStoreName);
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
    public void SelectShopObject_ShelfBuildsReadOnlyAggregateDetail()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Shelf,
            "ambient"));

        Assert.Equal(ManagementSection.Overview, viewModel.SelectedSection);
        Assert.NotNull(viewModel.SelectedShopObject);
        Assert.Equal("常温货架", viewModel.SelectedShopObject.Title);
        Assert.Equal("商品与库存", viewModel.SelectedShopObject.CategoryText);
        Assert.Contains("库存 0/20", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("缺货 1", viewModel.SelectedShopObject.StatusText);
    }

    [Fact]
    public void SelectShopObject_EmployeeBuildsReadOnlyOperationalDetail()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.SelectShopObjectCommand.Execute(Target(
            BusinessShopInteractionKind.Employee,
            "starter-cashier"));

        Assert.Equal(ManagementSection.Overview, viewModel.SelectedSection);
        Assert.NotNull(viewModel.SelectedShopObject);
        Assert.Equal("小葵", viewModel.SelectedShopObject.Title);
        Assert.Equal("收银员", viewModel.SelectedShopObject.CategoryText);
        Assert.Contains("效率 96%", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("工资 ¥4.00/小时", viewModel.SelectedShopObject.SummaryText);
        Assert.Contains("体力 100%", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("满意度 70%", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("班次", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("任务 导购", viewModel.SelectedShopObject.StatusText);
        Assert.Contains("优先级 收银 → 导购 → 待命", viewModel.SelectedShopObject.ActionHintText);
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

    private static BusinessShopInteractionTarget Target(
        BusinessShopInteractionKind kind,
        string key) =>
        new(kind, key, new LogicalPixelRect(0, 0, 1, 1));
}
