using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Game;

namespace HajimaoDesktopShop.Application.Tests.Business.Strategy;

public sealed class StoreStrategyPlannerTests
{
    [Theory]
    [InlineData(StorePricingPreset.HighTurnover, 9_000, 180)]
    [InlineData(StorePricingPreset.Balanced, 10_000, 200)]
    [InlineData(StorePricingPreset.HighMargin, 11_500, 230)]
    public void Create_AppliesWholeStoreReferencePriceMultiplier(
        StorePricingPreset preset,
        int expectedMultiplierBasisPoints,
        long expectedSalePriceCents)
    {
        var store = CreateStore(
            wholesalePriceCents: 100,
            referenceSalePriceCents: 200,
            capacity: 20);

        var plan = StoreStrategyPlanner.Create(store, preset, StoreStockingPreset.Balanced);

        var product = Assert.Single(plan.Products);
        Assert.Equal(expectedMultiplierBasisPoints, product.PriceMultiplierBasisPoints);
        Assert.Equal(expectedSalePriceCents, product.SalePriceCents);
        Assert.True(product.SalePriceCents > 100);
    }

    [Theory]
    [InlineData(StoreStockingPreset.Lean, 200, 550)]
    [InlineData(StoreStockingPreset.Balanced, 300, 750)]
    [InlineData(StoreStockingPreset.FullShelves, 450, 900)]
    public void Create_ConvertsStockingPresetToCapacityRelativeTargets(
        StoreStockingPreset preset,
        int reorderPermille,
        int targetPermille)
    {
        var plan = StoreStrategyPlanner.Create(
            CreateStore(capacity: 20),
            StorePricingPreset.Balanced,
            preset);

        var product = Assert.Single(plan.Products);
        Assert.Equal(Math.Max(1, 20 * reorderPermille / 1_000), product.ReorderPoint);
        Assert.Equal(Math.Max(1, 20 * targetPermille / 1_000), product.TargetQuantity);
        Assert.Equal("regional-distributor", product.PreferredChannelId);
        Assert.True(product.UseEmergencySupplierWhenOutOfStock);
    }

    [Fact]
    public void Create_ProtectsWholesalePriceWithFivePercentMinimumMarkup()
    {
        var plan = StoreStrategyPlanner.Create(
            CreateStore(wholesalePriceCents: 199, referenceSalePriceCents: 200),
            StorePricingPreset.HighTurnover,
            StoreStockingPreset.Balanced);

        Assert.Equal(208, Assert.Single(plan.Products).SalePriceCents);
    }

    [Fact]
    public void Create_UsesCheckedRoundedIntegerPricing()
    {
        var plan = StoreStrategyPlanner.Create(
            CreateStore(wholesalePriceCents: 1, referenceSalePriceCents: 199),
            StorePricingPreset.HighMargin,
            StoreStockingPreset.Balanced);

        Assert.Equal(229, Assert.Single(plan.Products).SalePriceCents);
    }

    [Fact]
    public void Create_ReturnsProductsInStableOrdinalOrder()
    {
        var store = CreateStore() with
        {
            Products =
            [
                Product("water"),
                Product("bread"),
                Product("apple")
            ]
        };

        var plan = StoreStrategyPlanner.Create(
            store,
            StorePricingPreset.Balanced,
            StoreStockingPreset.Balanced);

        Assert.Equal(["apple", "bread", "water"], plan.Products.Select(product => product.ProductId));
    }

    [Fact]
    public void Create_RejectsStoreWithoutProducts()
    {
        var store = CreateStore() with { Products = [] };

        Assert.Throws<ArgumentException>(() => StoreStrategyPlanner.Create(
            store,
            StorePricingPreset.Balanced,
            StoreStockingPreset.Balanced));
    }

    private static BusinessStoreSnapshot CreateStore(
        long wholesalePriceCents = 100,
        long referenceSalePriceCents = 200,
        int capacity = 20) =>
        new(
            "store-1",
            "街角店",
            RevenueCents: 0,
            StockPurchaseCostCents: 0,
            GrossProfitCents: 0,
            Products:
            [
                Product(
                    "water",
                    wholesalePriceCents,
                    referenceSalePriceCents,
                    capacity)
            ]);

    private static ProductSnapshot Product(
        string id,
        long wholesalePriceCents = 100,
        long referenceSalePriceCents = 200,
        int capacity = 20) =>
        new(
            id,
            id,
            wholesalePriceCents,
            referenceSalePriceCents,
            0,
            capacity,
            "ambient",
            ReferenceSalePriceCents: referenceSalePriceCents);
}
