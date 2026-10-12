using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Desktop.Tests.Integration;

public sealed class MultiStoreStreetAssignmentFlowTests
{
    [Fact]
    public void OpenClickAndAssign_UsesTwoRealStoresWithoutDuplicatingGlobalResources()
    {
        var session = MarketTestSession.Create(openingCashCents: 1_000_000);
        var proposal = session.CombatExpansion!.GetProposals()
            .Single(item => item.FormatId == "premium");
        Assert.Equal(CombatStoreExpansionStatus.Success, session.CombatExpansion.Open(proposal.BrandId).Status);
        var market = new MarketViewModel(session);

        var street = market.CommercialStreet.SceneFrame!.Snapshot;
        Assert.Equal(["corner-store", "store-0002"], street.Stores.Select(item => item.StoreId));
        Assert.NotEqual(street.Stores[0].FacadeStyleKey, street.Stores[1].FacadeStyleKey);
        Assert.Equal(480, CommercialStreetLayout.GetContentWidth(street.Stores.Count));
        Assert.Equal(
            new DesktopSize(480, 180),
            DesktopSurfaceWindowLayoutPolicy.Create(
                DesktopSurfaceMode.Street,
                street.Stores.Count,
                new DesktopRect(0, 0, 1920, 1040)).Size);

        market.DesktopNavigation.OpenStoreCommand.Execute("store-0002");

        Assert.Equal("store-0002", market.SelectedStoreId);
        Assert.Equal("store-0002", market.CombatDesktopFrame!.Scene.Snapshot.StoreId);
        Assert.Equal(DesktopSurfaceMode.Store, market.DesktopNavigation.Mode);
        Assert.True(market.CombatDesktopFrame.Scene.Snapshot.IsStaffed);

        market.EmployeeAssignment.AssignToStoreCommand.Execute("maomao-0001");
        market.Collection.EquipProductCommand.Execute("water");
        var combat = session.Combat!.GetSnapshot();

        Assert.False(combat.Stores.Single(item => item.StoreId == "corner-store").IsStaffed);
        Assert.True(combat.Stores.Single(item => item.StoreId == "store-0002").IsStaffed);
        Assert.Empty(combat.Loadouts.Single(item => item.StoreId == "corner-store").ProductIds);
        Assert.Equal(["water"], combat.Loadouts.Single(item => item.StoreId == "store-0002").ProductIds);
    }
}
