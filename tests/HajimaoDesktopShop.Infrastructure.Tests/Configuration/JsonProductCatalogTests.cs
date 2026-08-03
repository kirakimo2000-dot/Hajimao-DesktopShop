using HajimaoDesktopShop.Infrastructure.Configuration;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class JsonProductCatalogTests
{
    [Fact]
    public async Task LoadAsync_ShippedCatalog_HasTenUniqueProductsAndThreeShelfKinds()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "products.json");
        var catalog = new JsonProductCatalog(path);

        var products = await catalog.LoadAsync();

        Assert.Equal(10, products.Count);
        Assert.Equal(10, products.Select(product => product.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, products.Select(product => product.ShelfKind).Distinct(StringComparer.Ordinal).Count());
        Assert.All(products, product =>
        {
            Assert.True(product.WholesalePriceCents > 0);
            Assert.True(product.InitialSalePriceCents > product.WholesalePriceCents);
            Assert.True(product.Capacity > 0);
        });
    }

    [Fact]
    public async Task LoadAsync_WithDuplicateIds_ThrowsInvalidDataException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hajimao-products-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 1,
              "products": [
                { "id": "water", "name": "水", "wholesalePriceCents": 100, "initialSalePriceCents": 200, "capacity": 10, "shelfKind": "ambient" },
                { "id": "water", "name": "另一瓶水", "wholesalePriceCents": 110, "initialSalePriceCents": 220, "capacity": 10, "shelfKind": "ambient" }
              ]
            }
            """);

        try
        {
            var catalog = new JsonProductCatalog(path);
            await Assert.ThrowsAsync<InvalidDataException>(() => catalog.LoadAsync());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
