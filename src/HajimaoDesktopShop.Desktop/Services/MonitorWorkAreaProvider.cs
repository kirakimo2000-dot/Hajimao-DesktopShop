using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace HajimaoDesktopShop.Desktop.Services;

public static class MonitorWorkAreaProvider
{
    private const uint DefaultDpi = 96;

    public static IReadOnlyList<DesktopRect> GetLogicalWorkAreas()
    {
        var workAreas = new List<DesktopRect>();
        EnumDisplayMonitors(
            0,
            0,
            (nint monitor, nint _, ref NativeRect _, nint _) =>
            {
                var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(monitor, ref monitorInfo))
                {
                    return true;
                }

                var dpiX = DefaultDpi;
                var dpiY = DefaultDpi;
                if (GetDpiForMonitor(monitor, MonitorDpiType.Effective, out var detectedX, out var detectedY) == 0
                    && detectedX > 0
                    && detectedY > 0)
                {
                    dpiX = detectedX;
                    dpiY = detectedY;
                }

                var native = monitorInfo.WorkArea;
                if (native.Right > native.Left && native.Bottom > native.Top)
                {
                    workAreas.Add(ToLogicalRect(
                        native.Left,
                        native.Top,
                        native.Right,
                        native.Bottom,
                        dpiX,
                        dpiY));
                }

                return true;
            },
            0);

        return new ReadOnlyCollection<DesktopRect>(
            workAreas
                .OrderBy(workArea => workArea.X)
                .ThenBy(workArea => workArea.Y)
                .ToList());
    }

    public static DesktopRect ToLogicalRect(
        int left,
        int top,
        int right,
        int bottom,
        uint dpiX,
        uint dpiY)
    {
        if (right <= left)
        {
            throw new ArgumentOutOfRangeException(nameof(right), right, "Right must be greater than left.");
        }

        if (bottom <= top)
        {
            throw new ArgumentOutOfRangeException(nameof(bottom), bottom, "Bottom must be greater than top.");
        }

        if (dpiX == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX), dpiX, "DPI must be positive.");
        }

        if (dpiY == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiY), dpiY, "DPI must be positive.");
        }

        var scaleX = DefaultDpi / (double)dpiX;
        var scaleY = DefaultDpi / (double)dpiY;
        return new DesktopRect(
            left * scaleX,
            top * scaleY,
            (right - (double)left) * scaleX,
            (bottom - (double)top) * scaleY);
    }

    private delegate bool MonitorEnumProcedure(
        nint monitor,
        nint monitorDeviceContext,
        ref NativeRect monitorRectangle,
        nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRectangle,
        MonitorEnumProcedure callback,
        nint data);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint monitor,
        MonitorDpiType dpiType,
        out uint dpiX,
        out uint dpiY);

    private enum MonitorDpiType
    {
        Effective = 0
    }

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
