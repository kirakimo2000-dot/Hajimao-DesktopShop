using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopSurfaceWindowLayoutPolicy
{
    public const double WorkAreaMargin = 12d;
    public const double StoreWidth = 420d;
    public const double StoreHeight = 280d;

    public static DesktopSurfaceWindowLayout Create(
        DesktopSurfaceMode mode,
        int openedStoreCount,
        DesktopRect workArea)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(openedStoreCount, 1);
        var contentWidth = CommercialStreetLayout.GetContentWidth(openedStoreCount);
        var availableWidth = Math.Max(1d, workArea.Width - (WorkAreaMargin * 2d));
        var availableHeight = Math.Max(1d, workArea.Height - (WorkAreaMargin * 2d));
        var desiredWidth = mode == DesktopSurfaceMode.Street ? contentWidth : StoreWidth;
        var desiredHeight = mode == DesktopSurfaceMode.Street
            ? CommercialStreetLayout.LogicalHeight
            : StoreHeight;
        var size = new DesktopSize(
            Math.Min(desiredWidth, availableWidth),
            Math.Min(desiredHeight, availableHeight));
        var position = new DesktopPoint(
            workArea.X + ((workArea.Width - size.Width) / 2d),
            workArea.Bottom - size.Height - WorkAreaMargin);
        return new DesktopSurfaceWindowLayout(
            size,
            position,
            contentWidth,
            mode == DesktopSurfaceMode.Street && contentWidth > size.Width);
    }
}

public sealed record DesktopSurfaceWindowLayout(
    DesktopSize Size,
    DesktopPoint Position,
    int ContentWidth,
    bool RequiresHorizontalCamera);
