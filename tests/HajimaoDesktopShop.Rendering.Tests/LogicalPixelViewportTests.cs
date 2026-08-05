using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class LogicalPixelViewportTests
{
    [Theory]
    [InlineData(840, 360, 220, 100, 110, 50)]
    [InlineData(1000, 500, 290, 170, 105, 50)]
    public void TryMapPoint_UsesRendererIntegerScaleAndCentering(
        int width,
        int height,
        double x,
        double y,
        int expectedX,
        int expectedY)
    {
        var mapped = LogicalPixelViewport.TryMapPoint(
            width,
            height,
            420,
            180,
            x,
            y,
            out var point);

        Assert.True(mapped);
        Assert.Equal(new LogicalPixelPoint(expectedX, expectedY), point);
    }

    [Theory]
    [InlineData(1000, 500, 79, 100)]
    [InlineData(1000, 500, 920, 100)]
    [InlineData(1000, 500, 100, 69)]
    [InlineData(1000, 500, 100, 430)]
    public void TryMapPoint_RejectsCenteredLetterboxMargins(
        int width,
        int height,
        double x,
        double y)
    {
        Assert.False(LogicalPixelViewport.TryMapPoint(
            width,
            height,
            420,
            180,
            x,
            y,
            out _));
    }

    [Theory]
    [InlineData(0, 180)]
    [InlineData(420, 0)]
    [InlineData(-1, 180)]
    public void TryMapPoint_RejectsNonPositiveViewports(int width, int height)
    {
        Assert.False(LogicalPixelViewport.TryMapPoint(
            width,
            height,
            420,
            180,
            0,
            0,
            out _));
    }
}
