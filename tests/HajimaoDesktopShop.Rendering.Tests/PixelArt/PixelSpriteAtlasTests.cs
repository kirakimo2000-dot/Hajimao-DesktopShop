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
        Assert.Equal(4, atlas.GetFrames(PixelSpriteId.Cashier).Count);
        Assert.Equal(4, atlas.GetFrames(PixelSpriteId.Restocker).Count);
        Assert.Equal(4, atlas.GetFrames(PixelSpriteId.Customer).Count);
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfAmbient));
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfChilled));
        Assert.Single(atlas.GetFrames(PixelSpriteId.ShelfFrozen));
        Assert.Equal(10, atlas.ProductFrames.Count);
        Assert.All(
            Enum.GetValues<PixelSpriteId>().SelectMany(atlas.GetFrames),
            frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
        Assert.All(atlas.ProductFrames, frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
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
}
