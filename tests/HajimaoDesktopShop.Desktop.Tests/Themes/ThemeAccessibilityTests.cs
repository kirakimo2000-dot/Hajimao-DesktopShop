using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace HajimaoDesktopShop.Desktop.Tests.Themes;

public sealed class ThemeAccessibilityTests
{
    [Theory]
    [InlineData("Brush.Text", "Brush.Background", 7.0)]
    [InlineData("Brush.Text", "Brush.Surface", 7.0)]
    [InlineData("Brush.TextMuted", "Brush.Surface", 4.5)]
    [InlineData("Brush.TextMuted", "Brush.SurfaceElevated", 4.5)]
    [InlineData("Brush.OnPrimary", "Brush.Primary", 4.5)]
    [InlineData("Brush.Focus", "Brush.Background", 3.0)]
    public void SemanticPalette_MeetsDesktopContrastTargets(
        string foregroundKey,
        string backgroundKey,
        double minimumRatio)
    {
        var colors = ReadSolidColorBrushes();

        var ratio = ContrastRatio(colors[foregroundKey], colors[backgroundKey]);

        Assert.True(
            ratio >= minimumRatio,
            $"{foregroundKey} on {backgroundKey} has contrast {ratio:0.00}:1; expected at least {minimumRatio:0.0}:1.");
    }

    [Fact]
    public void SharedControls_UseAccessiblePixelInteractionTokens()
    {
        var xaml = File.ReadAllText(FindDesktopPath("Themes", "Controls.xaml"));

        Assert.Contains("x:Key=\"NavigationPixelButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"UtilityPixelButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PixelScrollThumb\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ScrollBar.PageLeftCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ScrollBar.PageRightCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SectionCard\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PageTitleText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"EyebrowText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"44\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Stroke=\"{DynamicResource Brush.Focus}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StrokeDashArray", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Value=\"#", xaml, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, Rgb> ReadSolidColorBrushes()
    {
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return XDocument.Load(FindDesktopPath("Themes", "Colors.xaml"))
            .Root!
            .Elements(presentation + "SolidColorBrush")
            .ToDictionary(
                element => element.Attribute(x + "Key")!.Value,
                element => ParseColor(element.Attribute("Color")!.Value),
                StringComparer.Ordinal);
    }

    private static Rgb ParseColor(string value)
    {
        var normalized = value.TrimStart('#');
        if (normalized.Length == 8)
        {
            normalized = normalized[2..];
        }

        return new Rgb(
            byte.Parse(normalized[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static double ContrastRatio(Rgb foreground, Rgb background)
    {
        var lighter = Math.Max(Luminance(foreground), Luminance(background));
        var darker = Math.Min(Luminance(foreground), Luminance(background));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Rgb color) =>
        0.2126 * Linearize(color.Red) +
        0.7152 * Linearize(color.Green) +
        0.0722 * Linearize(color.Blue);

    private static double Linearize(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static string FindDesktopPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(
            [directory.FullName, "src", "HajimaoDesktopShop.Desktop", .. segments]);
    }

    private sealed record Rgb(byte Red, byte Green, byte Blue);
}
