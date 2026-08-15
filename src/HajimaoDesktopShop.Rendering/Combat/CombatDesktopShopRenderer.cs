using HajimaoDesktopShop.Rendering.Animation;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Combat;

public sealed class CombatDesktopShopRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 280;
    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);
    private readonly SKBitmap _parts;
    private readonly CombatShopSceneRenderer _sceneRenderer;
    private readonly SKPaint _paint = new() { IsAntialias = false, Style = SKPaintStyle.Fill };
    private readonly SKTypeface _typeface =
        SKTypeface.FromFamilyName("Microsoft YaHei UI") ?? SKTypeface.Default;
    private readonly SKTypeface _monoTypeface =
        SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;
    private readonly SKFont _font;
    private readonly SKFont _smallFont;

    public CombatDesktopShopRenderer(SkeletalAnimationCatalog catalog, SKBitmap parts)
    {
        _parts = parts ?? throw new ArgumentNullException(nameof(parts));
        _sceneRenderer = new CombatShopSceneRenderer(catalog, parts);
        _font = new SKFont(_monoTypeface, 14f);
        _smallFont = new SKFont(_typeface, 12f);
    }

    public void Render(SKCanvas canvas, SKImageInfo target, CombatDesktopShopFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        canvas.Clear(SKColor.Parse("#17191D"));
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var scale = Math.Max(1, Math.Min(target.Width / LogicalWidth, target.Height / LogicalHeight));
        canvas.Save();
        canvas.Translate(
            (target.Width - LogicalWidth * scale) / 2,
            (target.Height - LogicalHeight * scale) / 2);
        canvas.Scale(scale);
        Fill(canvas, 0, 0, LogicalWidth, LogicalHeight, "#17191D");
        Fill(canvas, 0, 0, LogicalWidth, 42, "#23262C");
        Fill(canvas, 0, 238, LogicalWidth, 42, "#2D323A");
        Stroke(canvas, 0, 0, LogicalWidth, LogicalHeight, "#F1B844");
        Text(canvas, "HAJIMAO", 8, 26, "#F1B844", _font);
        Text(canvas, frame.CashText, 82, 26, "#F1B844", _font);
        Text(canvas, frame.PlayerLevelText, 196, 26, "#72C986", _smallFont);
        Button(canvas, 307, 5, 48, 34, frame.IsLocked ? "解锁" : "锁定", false);
        Button(canvas, 361, 5, 49, 34, "经营", true);

        using (var sceneBitmap = new SKBitmap(
                   CombatShopSceneRenderer.LogicalWidth,
                   CombatShopSceneRenderer.LogicalHeight,
                   SKColorType.Rgba8888,
                   SKAlphaType.Premul))
        using (var sceneCanvas = new SKCanvas(sceneBitmap))
        {
            _sceneRenderer.Render(sceneCanvas, sceneBitmap.Info, frame.Scene);
            using var sceneImage = SKImage.FromBitmap(sceneBitmap);
            canvas.DrawImage(
                sceneImage,
                new SKRect(0, 0, sceneBitmap.Width, sceneBitmap.Height),
                new SKRect(0, 50, LogicalWidth, 50 + CombatShopSceneRenderer.LogicalHeight),
                PixelSampling,
                _paint);
        }

        Fill(canvas, 7, 250, 4, 18, "#72C986");
        Text(canvas, frame.IncomeText, 16, 264, "#72C986", _smallFont);
        Text(canvas, frame.CustomerCountText, 252, 264, "#F4EBDD", _smallFont);
        Button(canvas, 364, 243, 48, 32, frame.IsClickThrough ? "恢复" : "穿透", false);
        canvas.Restore();
    }

    private void Button(SKCanvas canvas, int x, int y, int width, int height, string label, bool primary)
    {
        Fill(canvas, x, y, width, height, primary ? "#F1B844" : "#30353D");
        Stroke(canvas, x, y, width, height, primary ? "#FFE19A" : "#626975");
        Text(canvas, label, x + 9, y + 22, primary ? "#17191D" : "#F4EBDD", _smallFont);
    }

    private void Text(SKCanvas canvas, string value, float x, float y, string color, SKFont font)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawText(value, x, y, SKTextAlign.Left, font, _paint);
    }

    private void Fill(SKCanvas canvas, float x, float y, float width, float height, string color)
    {
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }

    private void Stroke(SKCanvas canvas, float x, float y, float width, float height, string color)
    {
        _paint.Style = SKPaintStyle.Stroke;
        _paint.StrokeWidth = 2;
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x + 1, y + 1, width - 2, height - 2, _paint);
        _paint.Style = SKPaintStyle.Fill;
    }

    public void Dispose()
    {
        _sceneRenderer.Dispose();
        _parts.Dispose();
        _font.Dispose();
        _smallFont.Dispose();
        _typeface.Dispose();
        if (!ReferenceEquals(_monoTypeface, _typeface))
        {
            _monoTypeface.Dispose();
        }

        _paint.Dispose();
    }
}
