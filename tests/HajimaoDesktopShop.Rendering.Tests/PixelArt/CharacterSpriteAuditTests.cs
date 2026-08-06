using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class CharacterSpriteAuditTests
{
    [Fact]
    public void Analyze_AcceptsOneConnectedSpriteWithTransparentPadding()
    {
        using var bitmap = TransparentBitmap();
        bitmap.SetPixel(3, 3, SKColors.White);
        bitmap.SetPixel(3, 4, SKColors.White);
        bitmap.SetPixel(4, 4, SKColors.White);

        var result = CharacterSpriteAudit.Analyze(bitmap, new PixelSpriteFrame(1, 1, 6, 6, 3, 6));

        Assert.Equal(3, result.VisiblePixelCount);
        Assert.Equal([3], result.ComponentSizes);
        Assert.Equal(2, result.LeftPadding);
        Assert.Equal(2, result.TopPadding);
        Assert.Equal(2, result.RightPadding);
        Assert.Equal(2, result.BottomPadding);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Analyze_RejectsDetachedPixelFragment()
    {
        using var bitmap = TransparentBitmap();
        bitmap.SetPixel(2, 2, SKColors.White);
        bitmap.SetPixel(2, 3, SKColors.White);
        bitmap.SetPixel(5, 5, SKColors.White);

        var result = CharacterSpriteAudit.Analyze(bitmap, new PixelSpriteFrame(1, 1, 6, 6, 3, 6));

        Assert.Equal([2, 1], result.ComponentSizes);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Analyze_RejectsBlankFrame()
    {
        using var bitmap = TransparentBitmap();

        var result = CharacterSpriteAudit.Analyze(bitmap, new PixelSpriteFrame(1, 1, 6, 6, 3, 6));

        Assert.Equal(0, result.VisiblePixelCount);
        Assert.Empty(result.ComponentSizes);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Analyze_RejectsSpriteClippedAgainstFrameEdge()
    {
        using var bitmap = TransparentBitmap();
        bitmap.SetPixel(1, 2, SKColors.White);
        bitmap.SetPixel(2, 2, SKColors.White);

        var result = CharacterSpriteAudit.Analyze(bitmap, new PixelSpriteFrame(1, 1, 6, 6, 3, 6));

        Assert.Equal(0, result.LeftPadding);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Analyze_RejectsFrameOutsideBitmap()
    {
        using var bitmap = TransparentBitmap();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CharacterSpriteAudit.Analyze(
                bitmap,
                new PixelSpriteFrame(4, 4, 6, 6, 3, 6)));
    }

    private static SKBitmap TransparentBitmap()
    {
        var bitmap = new SKBitmap(8, 8);
        bitmap.Erase(SKColors.Transparent);
        return bitmap;
    }
}
