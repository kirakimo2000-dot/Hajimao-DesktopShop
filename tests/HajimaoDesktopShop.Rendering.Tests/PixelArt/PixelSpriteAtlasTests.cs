using HajimaoDesktopShop.Rendering.PixelArt;

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
            atlas.GetFrames(PixelSpriteId.Cashier),
            frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
        Assert.All(atlas.ProductFrames, frame => Assert.True(atlas.ContainsVisiblePixels(frame)));
    }
}
