using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Desktop.Services;

public static class WindowInteractionService
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;

    public static void SetClickThrough(Window window, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(window);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        style = enabled ? style | WsExTransparent : style & ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExStyle, new nint(style));
    }

    public static void SnapToNearestWorkAreaCorner(Window window, double margin = 12d)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!TryGetWindowRect(window, out var currentWindow))
        {
            return;
        }

        if (DesktopWindowPlacementPolicy.TrySnapToNearestCorner(
                currentWindow,
                MonitorWorkAreaProvider.GetLogicalWorkAreas(),
                margin,
                out var snapped))
        {
            window.Left = snapped.X;
            window.Top = snapped.Y;
        }
    }

    public static bool TryRestorePlacement(
        Window window,
        DesktopWindowPlacement placement,
        double minimumVisible = 48d)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(placement);
        if (!double.IsFinite(placement.Left)
            || !double.IsFinite(placement.Top)
            || !double.IsFinite(minimumVisible)
            || minimumVisible <= 0d
            || !TryGetWindowSize(window, out var windowSize))
        {
            return false;
        }

        if (!DesktopWindowPlacementPolicy.TryRestore(
                new DesktopPoint(placement.Left, placement.Top),
                windowSize,
                MonitorWorkAreaProvider.GetLogicalWorkAreas(),
                minimumVisible,
                out var restored))
        {
            return false;
        }

        window.Left = restored.X;
        window.Top = restored.Y;
        return true;
    }

    private static bool TryGetWindowRect(Window window, out DesktopRect rectangle)
    {
        if (!double.IsFinite(window.Left)
            || !double.IsFinite(window.Top)
            || !TryGetWindowSize(window, out var size))
        {
            rectangle = default;
            return false;
        }

        rectangle = new DesktopRect(window.Left, window.Top, size.Width, size.Height);
        return true;
    }

    private static bool TryGetWindowSize(Window window, out DesktopSize size)
    {
        var width = window.ActualWidth > 0d ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0d ? window.ActualHeight : window.Height;
        if (!double.IsFinite(width)
            || width <= 0d
            || !double.IsFinite(height)
            || height <= 0d)
        {
            size = default;
            return false;
        }

        size = new DesktopSize(width, height);
        return true;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);

}
