using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class CommercialStreetSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 160;

    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };
    private readonly PixelSpriteAtlas _atlas;
    private readonly SKImage _atlasImage;

    public CommercialStreetSceneRenderer()
    {
        _atlas = PixelSpriteAtlas.LoadDefault();
        _atlasImage = SKImage.FromBitmap(_atlas.Bitmap);
    }

    public void Render(SKCanvas canvas, SKImageInfo target, CommercialStreetSceneFrame frame)
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
        canvas.ClipRect(new SKRect(0, 0, LogicalWidth, LogicalHeight));
        DrawLogicalScene(canvas, frame);
        canvas.Restore();
    }

    public void DrawLogicalScene(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        var street = frame.Snapshot;
        Fill(canvas, 0, 0, LogicalWidth, 34, SkyColor(street.Weather));
        Fill(canvas, 0, 34, LogicalWidth, 62, "#31262A");
        Fill(canvas, 0, 96, LogicalWidth, 28, "#B87349");
        Fill(canvas, 0, 124, LogicalWidth, 36, "#34383F");
        Fill(canvas, 0, 126, LogicalWidth, 3, "#F1B844");
        Fill(canvas, 0, 151, LogicalWidth, 3, "#D9D2C3");

        DrawStorefronts(canvas, street);
        DrawPedestrians(canvas, frame);
        DrawVehicles(canvas, street.VisibleVehicles);
        DrawWeather(canvas, street.Weather);
    }

    public void Dispose()
    {
        _atlasImage.Dispose();
        _atlas.Dispose();
        _paint.Dispose();
    }

    private void DrawStorefronts(SKCanvas canvas, CommercialStreetSnapshot street)
    {
        var unlockedSlots = CommercialStreetTrafficModel.GetUnlockedStorefrontCount(street.Tier);
        if (street.Stores.Count > unlockedSlots)
        {
            throw new InvalidOperationException(
                $"Commercial street tier '{street.Tier}' cannot render {street.Stores.Count} opened stores.");
        }

        for (var index = 0; index < 5; index++)
        {
            var x = 10 + index * 82;
            var isOpen = index < street.Stores.Count;
            var isUnlocked = index < unlockedSlots;
            Fill(canvas, x, 38, 72, 58, isOpen ? "#6B4634" : isUnlocked ? "#3D4650" : "#23262C");
            Fill(canvas, x + 6, 44, 60, 10, isOpen ? "#F1B844" : "#525A63");
            Fill(canvas, x + 8, 62, 22, 34, isOpen ? "#65B8C8" : "#2D323A");
            Fill(canvas, x + 42, 64, 18, 32, isOpen ? "#4A353C" : "#2D323A");
            if (isOpen)
            {
                var shareWidth = Math.Clamp(street.Stores[index].TrafficShareBasisPoints * 56 / 10_000, 1, 56);
                Fill(canvas, x + 8, 57, shareWidth, 3, "#72C986");
            }
        }
    }

    private void DrawPedestrians(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        var count = Math.Min(
            Math.Max(0, frame.Snapshot.VisiblePedestrians),
            PixelArtBudget.MaximumVisibleStreetPedestrians);
        var frames = _atlas.GetFrames(PixelSpriteId.Customer);
        var frameIndex = frame.ReduceMotion
            ? 0
            : (frame.AnimationFrame % frames.Count + frames.Count) % frames.Count;
        var sprite = frames[frameIndex];
        for (var index = 0; index < count; index++)
        {
            var anchorX = 34 + index * 62;
            var destination = new SKRect(
                anchorX - sprite.AnchorX,
                124 - sprite.AnchorY,
                anchorX - sprite.AnchorX + sprite.Width,
                124 - sprite.AnchorY + sprite.Height);
            var source = new SKRect(
                sprite.X,
                sprite.Y,
                sprite.X + sprite.Width,
                sprite.Y + sprite.Height);
            canvas.DrawImage(_atlasImage, source, destination, PixelSampling, _paint);
        }
    }

    private void DrawVehicles(SKCanvas canvas, int requestedCount)
    {
        var count = Math.Min(Math.Max(0, requestedCount), PixelArtBudget.MaximumVisibleStreetVehicles);
        for (var index = 0; index < count; index++)
        {
            var x = 40 + index * 210;
            Fill(canvas, x, 132, 54, 15, index == 0 ? "#E15A5A" : "#65B8C8");
            Fill(canvas, x + 10, 128, 30, 7, "#A6ABB4");
            Fill(canvas, x + 7, 145, 9, 5, "#17191D");
            Fill(canvas, x + 39, 145, 9, 5, "#17191D");
        }
    }

    private void DrawWeather(SKCanvas canvas, StreetWeather weather)
    {
        if (weather == StreetWeather.Rain)
        {
            for (var index = 0; index < 18; index++)
            {
                Fill(canvas, 8 + index * 24, 4 + index % 3 * 10, 2, 7, "#65B8C8");
            }
        }
        else if (weather == StreetWeather.Wind)
        {
            for (var index = 0; index < 5; index++)
            {
                Fill(canvas, 28 + index * 78, 14 + index % 2 * 9, 28, 2, "#A6ABB4");
            }
        }
    }

    private static string SkyColor(StreetWeather weather) => weather switch
    {
        StreetWeather.Clear => "#506878",
        StreetWeather.Cloudy => "#46515C",
        StreetWeather.Rain => "#31404B",
        StreetWeather.Wind => "#3F5664",
        _ => "#506878"
    };

    private void Fill(SKCanvas canvas, int x, int y, int width, int height, string color)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }
}
