using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

internal static class UiSnapshotRenderer
{
    public static void Render(Window window, int width, int height, string fileName)
    {
        AddTheme(window);
        window.ApplyTemplate();
        var surface = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        surface.Measure(new Size(width, height));
        surface.Arrange(new Rect(0, 0, width, height));
        surface.UpdateLayout();

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        Assert.True(pixels.Count(value => value != 0) > pixels.Length / 4);
        if (Environment.GetEnvironmentVariable("HAJIMAO_UI_SNAPSHOT_DIR") is not { Length: > 0 } outputDirectory)
        {
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(outputDirectory, fileName));
        encoder.Save(stream);
    }

    private static void AddTheme(Window window)
    {
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/HajimaoDesktopShop.Desktop;component/Themes/Colors.xaml",
                UriKind.Absolute)
        });
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/HajimaoDesktopShop.Desktop;component/Themes/Controls.xaml",
                UriKind.Absolute)
        });
    }
}
