using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public static class ProductDemandSelector
{
    public static int CalculateDemandWeight(
        ProductSnapshot product,
        int storeShelfWeightPermille,
        IReadOnlyDictionary<string, int> marketCategoryWeights)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(marketCategoryWeights);
        if (storeShelfWeightPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeShelfWeightPermille));
        }

        var shelfModifier = marketCategoryWeights.GetValueOrDefault(product.ShelfKind, 1_000);
        var categoryModifier = marketCategoryWeights.GetValueOrDefault(product.CategoryId, 1_000);
        return ScalePermille(ScalePermille(storeShelfWeightPermille, shelfModifier), categoryModifier);
    }

    public static ProductSnapshot Select(
        IReadOnlyList<ProductSnapshot> products,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(random);
        if (products.Count == 0 || products.Any(product => product.DemandWeightPermille <= 0))
        {
            throw new ArgumentException(
                "Product demand selection requires positive weighted products.",
                nameof(products));
        }

        var totalWeight = checked(products.Sum(product => product.DemandWeightPermille));
        var roll = random.Next(totalWeight);
        foreach (var product in products)
        {
            if (roll < product.DemandWeightPermille)
            {
                return product;
            }

            roll -= product.DemandWeightPermille;
        }

        throw new InvalidOperationException("Product demand selection exhausted its weights.");
    }

    private static int ScalePermille(int value, int modifierPermille) =>
        checked((int)(((long)value * modifierPermille) / 1_000));
}
