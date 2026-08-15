using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Interiors;

public sealed class StoreInteriorRenderer : IDisposable
{
    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);
    private readonly Dictionary<string, SKBitmap> _bitmaps = new(StringComparer.OrdinalIgnoreCase);

    public int Draw(SKCanvas canvas, string backgroundAssetPath, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (string.IsNullOrWhiteSpace(backgroundAssetPath))
        {
            throw new ArgumentException("Interior background path is required.", nameof(backgroundAssetPath));
        }

        if (!_bitmaps.TryGetValue(backgroundAssetPath, out var bitmap))
        {
            bitmap = SKBitmap.Decode(backgroundAssetPath)
                ?? throw new InvalidDataException($"Interior background '{backgroundAssetPath}' is unreadable.");
            _bitmaps.Add(backgroundAssetPath, bitmap);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        canvas.DrawImage(
            image,
            new SKRect(0, 0, bitmap.Width, bitmap.Height),
            new SKRect(0, 0, width, height),
            PixelSampling,
            paint);
        return 1;
    }

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps.Values)
        {
            bitmap.Dispose();
        }

        _bitmaps.Clear();
    }
}
