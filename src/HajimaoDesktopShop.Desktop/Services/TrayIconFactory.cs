using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HajimaoDesktopShop.Desktop.Services;

public static class TrayIconFactory
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            using var outline = new SolidBrush(Color.FromArgb(255, 23, 25, 29));
            using var wall = new SolidBrush(Color.FromArgb(255, 244, 235, 221));
            using var awning = new SolidBrush(Color.FromArgb(255, 241, 184, 68));
            using var awningDark = new SolidBrush(Color.FromArgb(255, 184, 115, 73));
            using var window = new SolidBrush(Color.FromArgb(255, 101, 184, 200));
            using var open = new SolidBrush(Color.FromArgb(255, 114, 201, 134));

            graphics.FillRectangle(outline, 3, 8, 26, 21);
            graphics.FillRectangle(wall, 5, 12, 22, 15);
            graphics.FillRectangle(awning, 3, 7, 26, 5);
            graphics.FillRectangle(awningDark, 3, 10, 5, 4);
            graphics.FillRectangle(awningDark, 13, 10, 5, 4);
            graphics.FillRectangle(awningDark, 23, 10, 6, 4);
            graphics.FillRectangle(outline, 7, 15, 9, 12);
            graphics.FillRectangle(window, 9, 17, 5, 6);
            graphics.FillRectangle(outline, 19, 16, 6, 7);
            graphics.FillRectangle(open, 20, 17, 4, 4);
            graphics.FillRectangle(awning, 7, 4, 18, 3);
        }

        var nativeHandle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(nativeHandle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(nativeHandle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
