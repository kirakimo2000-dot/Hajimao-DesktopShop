using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopSurfaceWindowLayoutPolicyTests
{
    private static readonly DesktopRect WorkArea = new(0, 0, 1920, 1040);

    [Fact]
    public void Street_UsesExactlyOneStorefrontWidthForTheInitialStore()
    {
        var layout = DesktopSurfaceWindowLayoutPolicy.Create(
            DesktopSurfaceMode.Street,
            1,
            WorkArea);

        Assert.Equal(new DesktopSize(248, 180), layout.Size);
        Assert.Equal(new DesktopPoint(836, 848), layout.Position);
    }

    [Fact]
    public void Street_GrowsWithEveryStoreWithoutCappingContentWidth()
    {
        var layout = DesktopSurfaceWindowLayoutPolicy.Create(
            DesktopSurfaceMode.Street,
            12,
            WorkArea);

        Assert.Equal(2800, layout.ContentWidth);
        Assert.Equal(1896, layout.Size.Width);
        Assert.True(layout.RequiresHorizontalCamera);
    }

    [Fact]
    public void Store_UsesTheExistingShopSurfaceSize()
    {
        var layout = DesktopSurfaceWindowLayoutPolicy.Create(
            DesktopSurfaceMode.Store,
            12,
            WorkArea);

        Assert.Equal(new DesktopSize(420, 280), layout.Size);
        Assert.False(layout.RequiresHorizontalCamera);
    }
}
