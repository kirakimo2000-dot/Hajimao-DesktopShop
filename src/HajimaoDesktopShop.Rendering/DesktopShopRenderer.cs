using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class DesktopShopRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 280;

    private readonly ShopSceneRenderer _sceneRenderer = new();
    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };
    private readonly SKTypeface _typeface =
        SKTypeface.FromFamilyName("Microsoft YaHei UI") ?? SKTypeface.Default;
    private readonly SKTypeface _monoTypeface =
        SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;
    private readonly SKFont _font;
    private readonly SKFont _smallFont;
    private readonly SKFont _monoFont;

    public DesktopShopRenderer()
    {
        _font = new SKFont(_typeface, 14f);
        _smallFont = new SKFont(_typeface, 12f);
        _monoFont = new SKFont(_monoTypeface, 14f);
    }

    public void Render(SKCanvas canvas, SKImageInfo target, DesktopShopFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        canvas.Clear(SKColor.Parse("#17191D"));
        var scale = Math.Max(1, Math.Min(target.Width / LogicalWidth, target.Height / LogicalHeight));
        var offsetX = (target.Width - (LogicalWidth * scale)) / 2;
        var offsetY = (target.Height - (LogicalHeight * scale)) / 2;
        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, LogicalWidth, LogicalHeight));

        Fill(canvas, 0, 0, LogicalWidth, LogicalHeight, "#17191D");
        Fill(canvas, 0, 0, LogicalWidth, 42, "#23262C");
        Fill(canvas, 0, 238, LogicalWidth, 42, "#2D323A");
        Stroke(canvas, 0, 0, LogicalWidth, LogicalHeight, "#F1B844", 2f);

        DrawText(canvas, "HAJIMAO", 8, 26, "#F1B844", _monoFont);
        DrawText(canvas, frame.CashText, 82, 26, "#F1B844", _monoFont);
        DrawText(canvas, frame.GameTimeText, 220, 26, "#F4EBDD", _smallFont);
        DrawButton(canvas, 307, 5, 48, 34, frame.IsLocked ? "解锁" : "锁定", isPrimary: false);
        DrawButton(canvas, 361, 5, 49, 34, "经营", isPrimary: true);

        canvas.Save();
        canvas.Translate(0, 50);
        _sceneRenderer.DrawLogicalScene(canvas, frame.Snapshot);
        canvas.Restore();

        Fill(canvas, 7, 250, 4, 18, "#E15A5A");
        DrawText(canvas, frame.StockWarningText, 16, 264, "#E15A5A", _smallFont);
        DrawText(canvas, frame.CustomerCountText, 252, 264, "#F4EBDD", _smallFont);
        DrawButton(
            canvas,
            364,
            243,
            48,
            32,
            frame.IsClickThrough ? "恢复" : "穿透",
            isPrimary: false);

        canvas.Restore();
    }

    public void Dispose()
    {
        _font.Dispose();
        _smallFont.Dispose();
        _monoFont.Dispose();
        _typeface.Dispose();
        if (!ReferenceEquals(_monoTypeface, _typeface))
        {
            _monoTypeface.Dispose();
        }

        _paint.Dispose();
        _sceneRenderer.Dispose();
    }

    private void DrawButton(
        SKCanvas canvas,
        int x,
        int y,
        int width,
        int height,
        string label,
        bool isPrimary)
    {
        Fill(canvas, x, y, width, height, isPrimary ? "#F1B844" : "#30353D");
        Stroke(canvas, x, y, width, height, isPrimary ? "#FFE19A" : "#626975", 2f);
        DrawText(canvas, label, x + 9, y + 22, isPrimary ? "#17191D" : "#F4EBDD", _smallFont);
    }

    private void DrawText(SKCanvas canvas, string text, float x, float baseline, string color, SKFont font)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawText(text, x, baseline, SKTextAlign.Left, font, _paint);
    }

    private void Fill(SKCanvas canvas, float x, float y, float width, float height, string color)
    {
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }

    private void Stroke(
        SKCanvas canvas,
        float x,
        float y,
        float width,
        float height,
        string color,
        float strokeWidth)
    {
        _paint.Style = SKPaintStyle.Stroke;
        _paint.StrokeWidth = strokeWidth;
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x + 1, y + 1, width - 2, height - 2, _paint);
        _paint.Style = SKPaintStyle.Fill;
    }
}
