using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Desktop.Services;

public static class WindowInteractionService
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const uint MonitorDefaultToNearest = 0x00000002;

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
        var handle = new WindowInteropHelper(window).Handle;
        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == 0 || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        var left = monitorInfo.WorkArea.Left / dpi.DpiScaleX;
        var top = monitorInfo.WorkArea.Top / dpi.DpiScaleY;
        var right = monitorInfo.WorkArea.Right / dpi.DpiScaleX;
        var bottom = monitorInfo.WorkArea.Bottom / dpi.DpiScaleY;
        var snapLeft = window.Left + (window.ActualWidth / 2d) < (left + right) / 2d;
        var snapTop = window.Top + (window.ActualHeight / 2d) < (top + bottom) / 2d;

        window.Left = snapLeft ? left + margin : right - window.ActualWidth - margin;
        window.Top = snapTop ? top + margin : bottom - window.ActualHeight - margin;
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
            || minimumVisible <= 0d)
        {
            return false;
        }

        var width = window.ActualWidth > 0d ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0d ? window.ActualHeight : window.Height;
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;
        var isVisible = placement.Left + width >= virtualLeft + minimumVisible
            && placement.Left <= virtualRight - minimumVisible
            && placement.Top + height >= virtualTop + minimumVisible
            && placement.Top <= virtualBottom - minimumVisible;
        if (!isVisible)
        {
            return false;
        }

        window.Left = placement.Left;
        window.Top = placement.Top;
        return true;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint windowHandle, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
