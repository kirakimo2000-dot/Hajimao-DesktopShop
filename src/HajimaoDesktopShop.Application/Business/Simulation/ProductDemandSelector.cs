using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public static class ProductDemandSelector
{
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
}
