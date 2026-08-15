using System.Text.Json;
using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class ShippedContentSpriteCoverageTests
{
    [Fact]
    public void ShippedContent_ResolvesEveryProductAndFacadeKey()
    {
        using var products = LoadJson("Assets", "Config", "products.json");
        using var brands = LoadJson("Assets", "Config", "store-brands.json");

        Assert.All(
            products.RootElement.GetProperty("products").EnumerateArray(),
            item => ContentSpriteKey.ResolveProduct(item.GetProperty("iconKey").GetString()!));
        Assert.All(
            brands.RootElement.GetProperty("brands").EnumerateArray(),
            item => ContentSpriteKey.ResolveFacade(item.GetProperty("facadeStyleKey").GetString()!));
    }

    private static JsonDocument LoadJson(params string[] pathSegments)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(
            [repositoryRoot, "src", "HajimaoDesktopShop.Desktop", .. pathSegments]);
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
