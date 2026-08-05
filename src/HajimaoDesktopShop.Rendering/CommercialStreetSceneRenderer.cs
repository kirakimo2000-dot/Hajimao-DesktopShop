using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class CommercialStreetSceneRenderer : IDisposable
{
    public const int LogicalHeight = CommercialStreetLayout.LogicalHeight;

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

        var contentWidth = CommercialStreetLayout.GetContentWidth(frame.Snapshot.Stores.Count);
        var scale = Math.Max(1, target.Height / LogicalHeight);
        var viewportWidth = Math.Min(contentWidth, Math.Max(1, target.Width / scale));
        var cameraOffset = CommercialStreetLayout.ClampCameraOffset(
            contentWidth,
            viewportWidth,
            frame.CameraOffset);
        canvas.Save();
        canvas.Translate(
            (target.Width - viewportWidth * scale) / 2,
            (target.Height - LogicalHeight * scale) / 2);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, viewportWidth, LogicalHeight));
        canvas.Translate(-cameraOffset, 0);
        DrawLogicalScene(canvas, frame);
        canvas.Restore();
    }

    public void DrawLogicalScene(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        var street = frame.Snapshot;
        var contentWidth = CommercialStreetLayout.GetContentWidth(street.Stores.Count);
        Fill(canvas, 0, 0, contentWidth, 28, SkyColor(street.Weather));
        Fill(canvas, 0, 28, contentWidth, 102, "#31262A");
        Fill(canvas, 0, 130, contentWidth, 20, "#B87349");
        Fill(canvas, 0, 150, contentWidth, 30, "#34383F");
        Fill(canvas, 0, 152, contentWidth, 3, "#F1B844");
        Fill(canvas, 0, 174, contentWidth, 3, "#D9D2C3");

        DrawStorefronts(canvas, street);
        DrawPedestrians(canvas, frame);
        DrawVehicles(canvas, street.VisibleVehicles, contentWidth, frame);
        DrawWeather(canvas, street.Weather, contentWidth);
    }

    public void Dispose()
    {
        _atlasImage.Dispose();
        _atlas.Dispose();
        _paint.Dispose();
    }

    private void DrawStorefronts(SKCanvas canvas, CommercialStreetSnapshot street)
    {
        var storefronts = CommercialStreetLayout.CreateStorefronts(street.Stores);
        for (var index = 0; index < storefronts.Count; index++)
        {
            var bounds = storefronts[index].Bounds;
            Fill(canvas, bounds.X, bounds.Y, bounds.Width, bounds.Height, "#6B4634");
            Fill(canvas, bounds.X + 8, bounds.Y + 7, bounds.Width - 16, 16, "#F1B844");
            Fill(canvas, bounds.X + 12, bounds.Y + 37, 72, bounds.Height - 37, "#65B8C8");
            Fill(canvas, bounds.X + 96, bounds.Y + 37, bounds.Width - 108, bounds.Height - 37, "#4A353C");
            var shareWidth = Math.Clamp(
                street.Stores[index].TrafficShareBasisPoints * (bounds.Width - 24) / 10_000,
                1,
                bounds.Width - 24);
            Fill(canvas, bounds.X + 12, bounds.Y + 27, shareWidth, 4, "#72C986");
        }
    }

    private void DrawPedestrians(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        var count = Math.Min(
            Math.Max(0, frame.Snapshot.VisiblePedestrians),
            PixelArtBudget.MaximumVisibleStreetPedestrians);
        var frames = _atlas.GetFrames(PixelSpriteId.Customer);
        var frameIndex = CharacterMotion.FrameIndex(
            frame.AnimationFrame,
            frames.Count,
            frame.ReduceMotion);
        var sprite = frames[frameIndex];
        var contentWidth = CommercialStreetLayout.GetContentWidth(frame.Snapshot.Stores.Count);
        for (var index = 0; index < count; index++)
        {
            var anchorX = CharacterMotion.HorizontalLoop(
                frame.AnimationFrame,
                index * 17,
                24,
                contentWidth - 24,
                4,
                frame.ReduceMotion);
            var destination = new SKRect(
                anchorX - sprite.AnchorX,
                150 - sprite.AnchorY,
                anchorX - sprite.AnchorX + sprite.Width,
                150 - sprite.AnchorY + sprite.Height);
            var source = new SKRect(
                sprite.X,
                sprite.Y,
                sprite.X + sprite.Width,
                sprite.Y + sprite.Height);
            canvas.DrawImage(_atlasImage, source, destination, PixelSampling, _paint);
        }
    }

    private void DrawVehicles(
        SKCanvas canvas,
        int requestedCount,
        int contentWidth,
        CommercialStreetSceneFrame frame)
    {
        var count = Math.Min(Math.Max(0, requestedCount), PixelArtBudget.MaximumVisibleStreetVehicles);
        for (var index = 0; index < count; index++)
        {
            var x = CharacterMotion.HorizontalLoop(
                frame.AnimationFrame,
                10 + index * 53,
                0,
                contentWidth - 56,
                4,
                frame.ReduceMotion);
            Fill(canvas, x, 158, 54, 15, index == 0 ? "#E15A5A" : "#65B8C8");
            Fill(canvas, x + 10, 154, 30, 7, "#A6ABB4");
            Fill(canvas, x + 7, 171, 9, 5, "#17191D");
            Fill(canvas, x + 39, 171, 9, 5, "#17191D");
        }
    }

    private void DrawWeather(SKCanvas canvas, StreetWeather weather, int contentWidth)
    {
        if (weather == StreetWeather.Rain)
        {
            for (var x = 8; x < contentWidth; x += 24)
            {
                Fill(canvas, x, 4 + (x / 24) % 3 * 10, 2, 7, "#65B8C8");
            }
        }
        else if (weather == StreetWeather.Wind)
        {
            for (var x = 28; x < contentWidth; x += 78)
            {
                Fill(canvas, x, 14 + (x / 78) % 2 * 9, 28, 2, "#A6ABB4");
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
