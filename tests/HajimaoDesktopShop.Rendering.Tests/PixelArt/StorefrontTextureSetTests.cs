using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class StorefrontTextureSetTests
{
    [Theory]
    [InlineData(FacadeGeometryKind.Convenience, "convenience.png")]
    [InlineData(FacadeGeometryKind.Commuter, "commuter.png")]
    [InlineData(FacadeGeometryKind.Discount, "discount.png")]
    [InlineData(FacadeGeometryKind.Warehouse, "warehouse.png")]
    [InlineData(FacadeGeometryKind.Lifestyle, "lifestyle.png")]
    [InlineData(FacadeGeometryKind.Health, "health.png")]
    [InlineData(FacadeGeometryKind.Premium, "premium.png")]
    [InlineData(FacadeGeometryKind.Variety, "variety.png")]
    public void FileName_MapsEveryFacadeKindToAStableTexture(
        FacadeGeometryKind kind,
        string expected)
    {
        Assert.Equal(expected, StorefrontTextureSet.FileName(kind));
    }

    [Theory]
    [InlineData("convenience.png")]
    [InlineData("commuter.png")]
    [InlineData("discount.png")]
    [InlineData("warehouse.png")]
    [InlineData("lifestyle.png")]
    [InlineData("health.png")]
    [InlineData("premium.png")]
    [InlineData("variety.png")]
    public void ShippedTexture_IsTransparentAndMatchesOneLogicalStorefront(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "facades", fileName);

        using var bitmap = SKBitmap.Decode(path);

        Assert.NotNull(bitmap);
        Assert.Equal(224, bitmap.Width);
        Assert.Equal(102, bitmap.Height);
        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.True(CountOpaquePixels(bitmap) > 4_000);
    }

    private static int CountOpaquePixels(SKBitmap bitmap)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
