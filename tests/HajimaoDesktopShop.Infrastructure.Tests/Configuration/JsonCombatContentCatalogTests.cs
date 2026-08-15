using HajimaoDesktopShop.Infrastructure.Configuration;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class JsonCombatContentCatalogTests
{
    [Fact]
    public async Task LoadAsync_ShippedCombatContent_IsCompleteAndReferenceSafe()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData");
        var catalog = new JsonCombatContentCatalog(
            Path.Combine(testData, "products.json"),
            Path.Combine(testData, "store-brands.json"),
            Path.Combine(testData, "product-combat.json"),
            Path.Combine(testData, "customer-archetypes.json"),
            Path.Combine(testData, "customer-spawn-pools.json"),
            Path.Combine(testData, "characters.json"),
            Path.Combine(testData, "interiors.json"));

        var content = await catalog.LoadAsync();

        Assert.True(content.Products.Count >= 24);
        Assert.True(content.Customers.Count >= 12);
        Assert.Equal(content.Products.Count, content.Products.Select(product => product.ProductId).Distinct().Count());
        Assert.Equal(content.Customers.Count, content.Customers.Select(customer => customer.Id).Distinct().Count());
        Assert.Equal(
            content.Products.Count,
            content.Products
                .Select(product => new
                {
                    product.BasePower,
                    product.AttackIntervalTicks,
                    product.RevenueModifierPermille,
                    product.Effect,
                    product.EffectStrengthPermille,
                    Tags = string.Join('|', product.Tags.Order(StringComparer.Ordinal)),
                    product.DropWeight
                })
                .Distinct()
                .Count());

        var maomao = Assert.Single(content.Characters);
        Assert.Equal("maomao-default", maomao.Id);
        Assert.Equal("humanoid-v1", maomao.RigId);
        Assert.Equal("maomao-default", maomao.SkinId);

        Assert.Equal(4, content.SpawnPools.Count);
        Assert.Equal(24, content.Interiors.Count);
        Assert.All(content.Interiors, interior => Assert.EndsWith(".png", interior.BackgroundAssetPath));

        var productIds = content.Products.Select(product => product.ProductId).ToHashSet(StringComparer.Ordinal);
        Assert.All(content.Customers, customer =>
        {
            Assert.True(customer.DemandHp > 0);
            Assert.InRange(customer.MovementPermillePerTick, 1, 10_000);
            Assert.True(customer.BaseRewardCents > 0);
            Assert.All(customer.ResistancePermille.Values, resistance => Assert.InRange(resistance, 0, 900));
            Assert.All(customer.ProductDropWeights.Keys, productId => Assert.Contains(productId, productIds));
        });
        var reachableDrops = content.Customers
            .SelectMany(customer => customer.ProductDropWeights.Keys)
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(productIds, productId => Assert.Contains(productId, reachableDrops));
    }
}
