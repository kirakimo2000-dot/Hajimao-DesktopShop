using System.IO;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class PixelSpriteAtlasTests
{
    [Fact]
    public void LoadDefault_ContainsEveryProductionFrameWithinBudget()
    {
        using var atlas = PixelSpriteAtlas.LoadDefault();

        Assert.Equal(256, atlas.Bitmap.Width);
        Assert.Equal(256, atlas.Bitmap.Height);
        Assert.InRange(atlas.EncodedByteCount, 1, PixelArtBudget.MaximumAtlasBytes);
        Assert.Equal(8, atlas.GetFrames(PixelSpriteId.Cashier).Count);
        Assert.Equal(8, atlas.GetFrames(PixelSpriteId.Restocker).Count);
        Assert.Equal(8, atlas.GetFrames(PixelSpriteId.Customer).Count);
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfAmbient));
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfChilled));
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfFrozen));
        Assert.Equal(10, atlas.ProductFrames.Count);
        Assert.All(
            Enum.GetValues<PixelSpriteId>().SelectMany(atlas.GetFrames),
            frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
        Assert.All(atlas.ProductFrames, frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
        Assert.All(
            new[]
            {
                PixelSpriteId.Cashier,
                PixelSpriteId.Restocker,
                PixelSpriteId.Customer
            }.SelectMany(atlas.GetFrames),
            frame => Assert.True(CharacterSpriteAudit.Analyze(atlas.Bitmap, frame).IsValid));
    }

    [Fact]
    public void GetCharacterFrame_UsesSharedTwentyFourFrameTimeline()
    {
        using var atlas = PixelSpriteAtlas.LoadDefault();

        var frame0 = atlas.GetCharacterFrame(PixelSpriteId.Customer, 0, reduceMotion: false);
        var frame1 = atlas.GetCharacterFrame(PixelSpriteId.Customer, 1, reduceMotion: false);
        var frame2 = atlas.GetCharacterFrame(PixelSpriteId.Customer, 2, reduceMotion: false);
        var frame3 = atlas.GetCharacterFrame(PixelSpriteId.Customer, 3, reduceMotion: false);
        var frame24 = atlas.GetCharacterFrame(PixelSpriteId.Customer, 24, reduceMotion: false);
        var reduced = atlas.GetCharacterFrame(PixelSpriteId.Customer, 23, reduceMotion: true);

        Assert.Equal(frame0, frame1);
        Assert.Equal(frame0, frame2);
        Assert.NotEqual(frame0, frame3);
        Assert.Equal(frame0, frame24);
        Assert.Equal(frame0, reduced);
    }

    [Fact]
    public void Load_RejectsAtlasWithWrongDimensions()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(encoded.ToArray());

        var exception = Assert.Throws<InvalidDataException>(() => PixelSpriteAtlas.Load(stream));

        Assert.Contains("256x256", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RejectsEncodedAtlasOverBudgetBeforeDecode()
    {
        using var stream = new MemoryStream(new byte[PixelArtBudget.MaximumAtlasBytes + 1]);

        var exception = Assert.Throws<InvalidDataException>(() => PixelSpriteAtlas.Load(stream));

        Assert.Contains("budget", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_RejectsDetachedPixelsInsideCharacterCel()
    {
        using var bitmap = new SKBitmap(256, 256);
        bitmap.Erase(SKColors.Transparent);
        foreach (var rowY in new[] { 0, 40, 80 })
        {
            for (var index = 0; index < 8; index++)
            {
                using var paint = new SKPaint { Color = SKColors.White };
                using var canvas = new SKCanvas(bitmap);
                canvas.DrawRect(index * 32 + 12, rowY + 10, 8, 20, paint);
            }
        }
        bitmap.SetPixel(4, 20, SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(encoded.ToArray());

        var exception = Assert.Throws<InvalidDataException>(() => PixelSpriteAtlas.Load(stream));

        Assert.Contains("Cashier", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cel 0", exception.Message, StringComparison.Ordinal);
    }
}
