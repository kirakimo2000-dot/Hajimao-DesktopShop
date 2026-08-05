using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopWindowPlacementPolicyTests
{
    [Theory]
    [InlineData(96, 1920, 1080)]
    [InlineData(120, 1536, 864)]
    [InlineData(144, 1280, 720)]
    [InlineData(192, 960, 540)]
    public void ToLogicalRect_ConvertsNativePixelsUsingEffectiveDpi(
        uint dpi,
        double expectedWidth,
        double expectedHeight)
    {
        var logical = MonitorWorkAreaProvider.ToLogicalRect(
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080,
            dpiX: dpi,
            dpiY: dpi);

        Assert.Equal(new DesktopRect(0, 0, expectedWidth, expectedHeight), logical);
    }

    [Fact]
    public void ToLogicalRect_PreservesNegativeOriginsAtScaledDpi()
    {
        var logical = MonitorWorkAreaProvider.ToLogicalRect(
            left: -1920,
            top: -2160,
            right: 0,
            bottom: 0,
            dpiX: 144,
            dpiY: 144);

        Assert.Equal(new DesktopRect(-1280, -1440, 1280, 1440), logical);
    }

    [Theory]
    [InlineData(0, 96)]
    [InlineData(96, 0)]
    public void ToLogicalRect_RejectsZeroDpi(uint dpiX, uint dpiY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MonitorWorkAreaProvider.ToLogicalRect(0, 0, 1920, 1080, dpiX, dpiY));
    }

    [Theory]
    [InlineData(0, 0, 0, 1080)]
    [InlineData(100, 0, 50, 1080)]
    [InlineData(0, 0, 1920, 0)]
    [InlineData(0, 100, 1920, 50)]
    public void ToLogicalRect_RejectsNonPositiveNativeRectangle(
        int left,
        int top,
        int right,
        int bottom)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MonitorWorkAreaProvider.ToLogicalRect(left, top, right, bottom, 96, 96));
    }

    [Fact]
    public void Geometry_RejectsNonFiniteCoordinatesAndNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopPoint(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopPoint(0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopSize(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopSize(100, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopRect(0, 0, double.NaN, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DesktopRect(0, 0, 100, 0));
    }

    [Fact]
    public void TryRestore_AcceptsNegativeCoordinateMonitor()
    {
        var workAreas = new[]
        {
            new DesktopRect(-1920, 0, 1920, 1080),
            new DesktopRect(0, 0, 2560, 1400)
        };

        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(-1800, 20),
            new DesktopSize(420, 280),
            workAreas,
            minimumVisible: 48,
            out var point);

        Assert.True(restored);
        Assert.Equal(new DesktopPoint(-1800, 20), point);
    }

    [Fact]
    public void TryRestore_RejectsWindowInGapInsideVirtualBoundingRectangle()
    {
        var workAreas = new[]
        {
            new DesktopRect(0, 0, 1920, 1080),
            new DesktopRect(1920, 1500, 1920, 1080)
        };

        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(2000, 1100),
            new DesktopSize(420, 280),
            workAreas,
            minimumVisible: 48,
            out _);

        Assert.False(restored);
    }

    [Theory]
    [InlineData(952, true)]
    [InlineData(953, false)]
    public void TryRestore_RequiresMinimumVisibilityOnBothAxes(double left, bool expected)
    {
        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(left, 200),
            new DesktopSize(420, 280),
            [new DesktopRect(0, 0, 1000, 800)],
            minimumVisible: 48,
            out _);

        Assert.Equal(expected, restored);
    }

    [Fact]
    public void TryRestore_AcceptsVerticallyArrangedMonitor()
    {
        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(120, -1000),
            new DesktopSize(420, 280),
            [new DesktopRect(0, -1080, 1920, 1080), new DesktopRect(0, 0, 1920, 1040)],
            minimumVisible: 48,
            out var point);

        Assert.True(restored);
        Assert.Equal(new DesktopPoint(120, -1000), point);
    }

    [Fact]
    public void TryRestore_RejectsPlacementFromRemovedMonitor()
    {
        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(2100, 200),
            new DesktopSize(420, 280),
            [new DesktopRect(0, 0, 1920, 1040)],
            minimumVisible: 48,
            out _);

        Assert.False(restored);
    }

    [Fact]
    public void TryRestore_ReturnsFalseForNoWorkAreas()
    {
        var restored = DesktopWindowPlacementPolicy.TryRestore(
            new DesktopPoint(0, 0),
            new DesktopSize(420, 280),
            [],
            minimumVisible: 48,
            out _);

        Assert.False(restored);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void TryRestore_RejectsInvalidMinimumVisibility(double minimumVisible)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopWindowPlacementPolicy.TryRestore(
                new DesktopPoint(0, 0),
                new DesktopSize(420, 280),
                [new DesktopRect(0, 0, 1920, 1040)],
                minimumVisible,
                out _));
    }

    [Theory]
    [InlineData(10, 10, 12, 12)]
    [InlineData(700, 10, 568, 12)]
    [InlineData(10, 500, 12, 508)]
    [InlineData(700, 500, 568, 508)]
    public void TrySnapToNearestCorner_SelectsAllFourCorners(
        double currentLeft,
        double currentTop,
        double expectedLeft,
        double expectedTop)
    {
        var snapped = DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
            new DesktopRect(currentLeft, currentTop, 420, 280),
            [new DesktopRect(0, 0, 1000, 800)],
            margin: 12,
            out var point);

        Assert.True(snapped);
        Assert.Equal(new DesktopPoint(expectedLeft, expectedTop), point);
    }

    [Fact]
    public void TrySnapToNearestCorner_UsesNearestWorkAreaByWindowCenter()
    {
        var snapped = DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
            new DesktopRect(-300, 100, 420, 280),
            [new DesktopRect(0, 0, 1920, 1040), new DesktopRect(-1920, 0, 1920, 1080)],
            margin: 12,
            out var point);

        Assert.True(snapped);
        Assert.Equal(new DesktopPoint(-432, 12), point);
    }

    [Fact]
    public void TrySnapToNearestCorner_CentersOversizedWindowOnWorkArea()
    {
        var snapped = DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
            new DesktopRect(10, 10, 1200, 900),
            [new DesktopRect(0, 0, 1000, 800)],
            margin: 12,
            out var point);

        Assert.True(snapped);
        Assert.Equal(new DesktopPoint(-100, -50), point);
    }

    [Fact]
    public void TrySnapToNearestCorner_ReturnsFalseForNoWorkAreas()
    {
        var snapped = DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
            new DesktopRect(0, 0, 420, 280),
            [],
            margin: 12,
            out _);

        Assert.False(snapped);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void TrySnapToNearestCorner_RejectsInvalidMargin(double margin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
                new DesktopRect(0, 0, 420, 280),
                [new DesktopRect(0, 0, 1920, 1040)],
                margin,
                out _));
    }
}
