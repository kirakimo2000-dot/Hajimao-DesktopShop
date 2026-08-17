using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class MarketViewModelTests
{
    [Fact]
    public void NewSession_ProjectsCombatStreetAndOnePlainNextStep()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        Assert.Equal("corner-store", viewModel.SelectedStoreId);
        Assert.NotNull(viewModel.CombatDesktopFrame);
        Assert.Equal("累计收益 ¥0.00", viewModel.StockWarningText);
        Assert.Equal("等待毛毛完成首位顾客", viewModel.NextAction.Title);
        Assert.Equal(3, viewModel.Loadout.Slots.Count);
        Assert.Single(viewModel.Collection.Products);
    }

    [Fact]
    public void Navigation_ChangesOnlyTheVisibleManagementSection()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.NavigateCommand.Execute(ManagementSection.Strategy);

        Assert.True(viewModel.IsStrategySection);
        Assert.NotNull(viewModel.CombatDesktopFrame);
    }

    [Fact]
    public void SelectingLockedStore_DoesNotCreateCombatOrCrash()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var locked = viewModel.Stores.Single(store => store.Id == "station-store");

        viewModel.SelectStoreCommand.Execute(locked);

        Assert.Equal("station-store", viewModel.SelectedStoreId);
        Assert.Null(viewModel.CombatDesktopFrame);
        Assert.Empty(viewModel.Loadout.Slots);
    }

    [Fact]
    public void Refresh_DoesNotAdvanceLegacySimulationOrAwardOfflineIncome()
    {
        var session = MarketTestSession.Create();
        var viewModel = new MarketViewModel(session);
        var minute = session.Simulation.GetSnapshot().GameMinute;
        var cash = session.Game.GetSnapshot().CashCents;

        viewModel.Refresh();
        viewModel.Refresh();

        Assert.Equal(minute, session.Simulation.GetSnapshot().GameMinute);
        Assert.Equal(cash, session.Game.GetSnapshot().CashCents);
    }

    [Fact]
    public void PresentationRefresh_DoesNotRebuildManagementCollections()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());
        var collectionChanges = 0;
        var loadoutChanges = 0;
        var investmentChanges = 0;
        viewModel.Collection.Products.CollectionChanged += (_, _) => collectionChanges++;
        viewModel.Loadout.Slots.CollectionChanged += (_, _) => loadoutChanges++;
        viewModel.Investment.Candidates.CollectionChanged += (_, _) => investmentChanges++;

        viewModel.RefreshPresentation();

        Assert.Equal(0, collectionChanges);
        Assert.Equal(0, loadoutChanges);
        Assert.Equal(0, investmentChanges);
        Assert.NotNull(viewModel.CombatDesktopFrame);
    }

    [Fact]
    public void DesktopControls_KeepLockAndMouseThroughIndependent()
    {
        var viewModel = new MarketViewModel(MarketTestSession.Create());

        viewModel.ToggleLockCommand.Execute(null);
        viewModel.ToggleClickThroughCommand.Execute(null);

        Assert.True(viewModel.IsLocked);
        Assert.True(viewModel.IsClickThrough);
    }
}
