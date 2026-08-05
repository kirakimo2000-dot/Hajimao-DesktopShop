using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class DesktopNavigationViewModelTests
{
    [Fact]
    public void StartsOnStreetAndOnlyOpenedStoreCanBeEntered()
    {
        var selected = string.Empty;
        var navigation = new DesktopNavigationViewModel(id => selected = id);
        navigation.Synchronize(
        [
            Store("corner", "街角店", isOpen: true),
            Store("station", "车站店", isOpen: false)
        ], "corner");

        navigation.OpenStoreCommand.Execute("station");

        Assert.Equal(DesktopSurfaceMode.Street, navigation.Mode);
        Assert.Equal(string.Empty, selected);

        navigation.OpenStoreCommand.Execute("corner");

        Assert.Equal(DesktopSurfaceMode.Store, navigation.Mode);
        Assert.Equal("corner", selected);
        Assert.Equal("街角店", navigation.PageTitle);
    }

    [Fact]
    public void BackToStreetKeepsTheSelectedStoreContext()
    {
        var selected = string.Empty;
        var navigation = new DesktopNavigationViewModel(id => selected = id);
        navigation.Synchronize([Store("corner", "街角店", isOpen: true)], "corner");
        navigation.OpenStoreCommand.Execute("corner");

        navigation.BackToStreetCommand.Execute(null);

        Assert.Equal(DesktopSurfaceMode.Street, navigation.Mode);
        Assert.Equal("corner", selected);
        Assert.Equal("街区", navigation.PageTitle);
    }

    [Fact]
    public void SynchronizeReturnsToStreetIfTheDisplayedStoreIsNoLongerOpen()
    {
        var navigation = new DesktopNavigationViewModel(_ => { });
        navigation.Synchronize([Store("corner", "街角店", isOpen: true)], "corner");
        navigation.OpenStoreCommand.Execute("corner");

        navigation.Synchronize([Store("corner", "街角店", isOpen: false)], "corner");

        Assert.Equal(DesktopSurfaceMode.Street, navigation.Mode);
    }

    private static StoreNavigationItemViewModel Store(string id, string name, bool isOpen) =>
        new(new StoreCatalogItemSnapshot(id, name, 1, 0, isOpen));
}
