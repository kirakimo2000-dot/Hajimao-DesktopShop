using HajimaoDesktopShop.Infrastructure.Configuration;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class JsonStoreContentCatalogTests
{
    [Fact]
    public async Task LoadAsync_ShippedCatalog_HasFourFormatsAndTwentyFourReferencedBrands()
    {
        var formatsPath = Path.Combine(AppContext.BaseDirectory, "TestData", "store-formats.json");
        var brandsPath = Path.Combine(AppContext.BaseDirectory, "TestData", "store-brands.json");
        var catalog = new JsonStoreContentCatalog(formatsPath, brandsPath);

        var content = await catalog.LoadAsync();

        Assert.Equal(4, content.Formats.Count);
        Assert.Equal(24, content.Brands.Count);
        Assert.Equal(24, content.Brands.Select(brand => brand.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(content.Brands, brand => brand.DisplayName == "7-Eleven");
        Assert.Contains(content.Brands, brand => brand.DisplayName == "银座三越");
        Assert.All(content.Brands, brand =>
            Assert.Contains(content.Formats, format => format.Id == brand.FormatId));
        Assert.All(content.Formats, format =>
            Assert.True(content.Brands.Count(brand => brand.FormatId == format.Id) >= 5));
        Assert.All(content.Formats, format =>
        {
            Assert.True(format.BaseOpeningCostCents >= 0);
            Assert.True(format.RecommendedReserveCents > 0);
            Assert.Equal(3, format.ProductShelfWeights.Count);
            Assert.Contains("ambient", format.ProductShelfWeights.Keys);
            Assert.Contains("chilled", format.ProductShelfWeights.Keys);
            Assert.Contains("frozen", format.ProductShelfWeights.Keys);
        });
    }
}
