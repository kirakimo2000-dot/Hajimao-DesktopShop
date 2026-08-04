using System.Collections.ObjectModel;
using System.Reflection;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.PixelArt;

public sealed class PixelSpriteAtlas : IDisposable
{
    private const string DefaultResourceName =
        "HajimaoDesktopShop.Rendering.Assets.PixelArt.market-atlas.png";

    private readonly IReadOnlyDictionary<PixelSpriteId, IReadOnlyList<PixelSpriteFrame>> _frames;

    private PixelSpriteAtlas(SKBitmap bitmap, int encodedByteCount)
    {
        Bitmap = bitmap;
        EncodedByteCount = encodedByteCount;
        ProductFrames = Array.AsReadOnly(
            Enumerable.Range(0, 10)
                .Select(index => new PixelSpriteFrame(index * 16, 176, 16, 16, 8, 16))
                .ToArray());
        _frames = new ReadOnlyDictionary<PixelSpriteId, IReadOnlyList<PixelSpriteFrame>>(
            new Dictionary<PixelSpriteId, IReadOnlyList<PixelSpriteFrame>>
            {
                [PixelSpriteId.Cashier] = CharacterFrames(0),
                [PixelSpriteId.Restocker] = CharacterFrames(40),
                [PixelSpriteId.Customer] = CharacterFrames(80),
                [PixelSpriteId.ShelfAmbient] = SingleFrame(0, 120, 64, 56),
                [PixelSpriteId.ShelfChilled] = SingleFrame(64, 120, 64, 56),
                [PixelSpriteId.ShelfFrozen] = SingleFrame(128, 120, 64, 56)
            });
    }

    public SKBitmap Bitmap { get; }

    public int EncodedByteCount { get; }

    public IReadOnlyList<PixelSpriteFrame> ProductFrames { get; }

    public static PixelSpriteAtlas LoadDefault()
    {
        var assembly = typeof(PixelSpriteAtlas).Assembly;
        using var stream = assembly.GetManifestResourceStream(DefaultResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded pixel atlas '{DefaultResourceName}' was not found in {assembly.GetName().Name}.");
        return Load(stream);
    }

    public static PixelSpriteAtlas Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        if (buffer.Length > PixelArtBudget.MaximumAtlasBytes)
        {
            throw new InvalidDataException(
                $"Pixel atlas exceeds the {PixelArtBudget.MaximumAtlasBytes}-byte budget.");
        }

        var encodedBytes = buffer.ToArray();
        var bitmap = SKBitmap.Decode(encodedBytes)
            ?? throw new InvalidDataException("The pixel atlas could not be decoded.");
        if (bitmap.Width != PixelArtBudget.AtlasWidth || bitmap.Height != PixelArtBudget.AtlasHeight)
        {
            var actualDimensions = $"{bitmap.Width}x{bitmap.Height}";
            bitmap.Dispose();
            throw new InvalidDataException(
                $"Pixel atlas must be {PixelArtBudget.AtlasWidth}x{PixelArtBudget.AtlasHeight}; "
                + $"received {actualDimensions}.");
        }

        return new PixelSpriteAtlas(bitmap, encodedBytes.Length);
    }

    public IReadOnlyList<PixelSpriteFrame> GetFrames(PixelSpriteId spriteId) => _frames[spriteId];

    public bool ContainsVisiblePixels(PixelSpriteFrame frame)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame.X);
        ArgumentOutOfRangeException.ThrowIfNegative(frame.Y);
        if (frame.X + frame.Width > Bitmap.Width || frame.Y + frame.Height > Bitmap.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Sprite frame falls outside the atlas.");
        }

        for (var y = frame.Y; y < frame.Y + frame.Height; y++)
        {
            for (var x = frame.X; x < frame.X + frame.Width; x++)
            {
                if (Bitmap.GetPixel(x, y).Alpha > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose() => Bitmap.Dispose();

    private static IReadOnlyList<PixelSpriteFrame> CharacterFrames(int y) =>
        Array.AsReadOnly(
            Enumerable.Range(0, PixelArtBudget.CharacterFrameCount)
                .Select(index => new PixelSpriteFrame(index * 32, y, 32, 40, 16, 40))
                .ToArray());

    private static IReadOnlyList<PixelSpriteFrame> SingleFrame(int x, int y, int width, int height) =>
        Array.AsReadOnly([new PixelSpriteFrame(x, y, width, height, width / 2, height)]);
}
