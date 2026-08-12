using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class ContentSpriteKeyTests
{
    [Theory]
    [InlineData("product-beverage-water")]
    [InlineData("product-stationery-notes")]
    [InlineData("product-home-gift-candle")]
    public void ProductIcon_MapsRichKeysToSharedVisibleFrames(string key)
    {
        var variant = ContentSpriteKey.ResolveProduct(key);

        Assert.InRange(variant.FrameIndex, 0, 9);
        Assert.InRange(variant.PaletteIndex, 0, 7);
    }

    [Theory]
    [InlineData("facade-convenience-a")]
    [InlineData("facade-premium-d")]
    [InlineData("facade-health-c")]
    public void Facade_MapsFormatAndStyleToCombinatorialVariant(string key)
    {
        var variant = ContentSpriteKey.ResolveFacade(key);

        Assert.InRange(variant.PaletteIndex, 0, 7);
        Assert.InRange(variant.AwningIndex, 0, 3);
    }

    [Theory]
    [InlineData("employee-a01")]
    [InlineData("employee-f08")]
    [InlineData("employee-l08")]
    public void EmployeeAppearance_UsesTwentyFourLogicalFramesWithoutDuplicatingCels(string key)
    {
        var variant = ContentSpriteKey.ResolveEmployee(key);

        Assert.Equal(24, variant.LogicalFrameCount);
        Assert.Equal(8, variant.StoredCelCount);
        Assert.InRange(variant.PaletteIndex, 0, 11);
        Assert.InRange(variant.DetailIndex, 0, 7);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-product")]
    public void ResolveProduct_RejectsUnknownKeyShape(string key) =>
        Assert.Throws<ArgumentException>(() => ContentSpriteKey.ResolveProduct(key));
}
