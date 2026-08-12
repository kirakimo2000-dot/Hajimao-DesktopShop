using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

public sealed class ProductDemandSelectorTests
{
    [Fact]
    public void Select_UsesConfiguredDemandWeightsRatherThanUniformIndexes()
    {
        var products = new[]
        {
            Product("ambient", 500),
            Product("chilled", 1_500)
        };
        var random = new FixedIntegerRandomSource(1_999);

        var selected = ProductDemandSelector.Select(products, random);

        Assert.Equal("chilled", selected.Id);
        Assert.Equal(2_000, random.LastExclusiveMaximum);
    }

    [Fact]
    public void Select_RejectsEmptyOrNonPositiveWeightedProducts()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductDemandSelector.Select([], new FixedIntegerRandomSource(0)));
        Assert.Throws<ArgumentException>(() =>
            ProductDemandSelector.Select([Product("invalid", 0)], new FixedIntegerRandomSource(0)));
    }

    [Fact]
    public void CalculateDemandWeight_CombinesStoreShelfAndMarketProductCategory()
    {
        var product = new ProductSnapshot(
            "tea",
            "茶饮",
            100,
            200,
            1,
            10,
            "ambient",
            CategoryId: "beverages");
        var marketWeights = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["ambient"] = 1_100,
            ["beverages"] = 1_500
        };

        var weight = ProductDemandSelector.CalculateDemandWeight(product, 1_200, marketWeights);

        Assert.Equal(1_980, weight);
    }

    private static ProductSnapshot Product(string id, int weight) =>
        new(id, id, 100, 200, 1, 10, id, DemandWeightPermille: weight);

    private sealed class FixedIntegerRandomSource(int value) : IRandomSource
    {
        public int LastExclusiveMaximum { get; private set; }

        public double NextDouble() => 0;

        public int Next(int exclusiveMax)
        {
            LastExclusiveMaximum = exclusiveMax;
            return Math.Clamp(value, 0, exclusiveMax - 1);
        }
    }
}
