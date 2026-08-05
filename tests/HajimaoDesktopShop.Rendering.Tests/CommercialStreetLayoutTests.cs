using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class CommercialStreetLayoutTests
{
    [Theory]
    [InlineData(1, 248)]
    [InlineData(2, 480)]
    [InlineData(5, 1176)]
    [InlineData(12, 2800)]
    public void ContentWidth_GrowsWithEveryOpenedStoreWithoutACap(int storeCount, int expectedWidth)
    {
        Assert.Equal(expectedWidth, CommercialStreetLayout.GetContentWidth(storeCount));
    }

    [Fact]
    public void StorefrontsUseContentCoordinatesAndExclusiveRightBoundary()
    {
        var storefronts = CommercialStreetLayout.CreateStorefronts(
        [
            Store("corner"),
            Store("station")
        ]);

        Assert.Equal(new LogicalPixelRect(12, 28, 224, 102), storefronts[0].Bounds);
        Assert.Equal(new LogicalPixelRect(244, 28, 224, 102), storefronts[1].Bounds);
        Assert.Equal("corner", CommercialStreetLayout.HitTest(storefronts, 12, 28));
        Assert.Equal("corner", CommercialStreetLayout.HitTest(storefronts, 235, 129));
        Assert.Null(CommercialStreetLayout.HitTest(storefronts, 236, 129));
        Assert.Equal("station", CommercialStreetLayout.HitTest(storefronts, 244, 28));
    }

    [Theory]
    [InlineData(2800, 1000, -50, 0)]
    [InlineData(2800, 1000, 0, 0)]
    [InlineData(2800, 1000, 900, 900)]
    [InlineData(2800, 1000, 5000, 1800)]
    [InlineData(480, 1000, 500, 0)]
    public void CameraOffset_ClampsToTheUnboundedContentViewport(
        int contentWidth,
        int viewportWidth,
        int requested,
        int expected)
    {
        Assert.Equal(
            expected,
            CommercialStreetLayout.ClampCameraOffset(contentWidth, viewportWidth, requested));
    }

    [Fact]
    public void ContentWidth_RejectsAnEmptyStreet()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommercialStreetLayout.GetContentWidth(0));
    }

    private static CommercialStreetStoreSnapshot Store(string id) =>
        new(id, id, 10_000, 10_000);
}
