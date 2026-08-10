using HajimaoDesktopShop.Application.Game;

namespace HajimaoDesktopShop.Application.Business.Strategy;

public static class StoreStrategyPlanner
{
    private const string RegionalDistributorId = "regional-distributor";

    public static StoreStrategyPlan Create(
        BusinessStoreSnapshot store,
        StorePricingPreset pricing,
        StoreStockingPreset stocking)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (store.Products.Count == 0)
        {
            throw new ArgumentException("A store strategy requires at least one product.", nameof(store));
        }

        var priceMultiplier = PriceMultiplier(pricing);
        var (reorderPermille, targetPermille) = StockingThresholds(stocking);
        var products = store.Products
            .OrderBy(product => product.Id, StringComparer.Ordinal)
            .Select(product => CreateProductPlan(
                product,
                priceMultiplier,
                reorderPermille,
                targetPermille))
            .ToArray();

        return new StoreStrategyPlan(
            store.Id,
            pricing,
            stocking,
            Array.AsReadOnly(products));
    }

    private static StoreProductStrategyPlan CreateProductPlan(
        ProductSnapshot product,
        int priceMultiplier,
        int reorderPermille,
        int targetPermille)
    {
        var referencePrice = product.ReferenceSalePriceCents > 0
            ? product.ReferenceSalePriceCents
            : product.SalePriceCents;
        var proposedPrice = checked((referencePrice * priceMultiplier + 5_000) / 10_000);
        var minimumMarkup = Math.Max(1L, product.WholesalePriceCents / 20);
        var minimumPrice = checked(product.WholesalePriceCents + minimumMarkup);
        var salePrice = Math.Max(proposedPrice, minimumPrice);
        var reorderPoint = Math.Max(1, checked(product.Capacity * reorderPermille / 1_000));
        var targetQuantity = Math.Max(1, checked(product.Capacity * targetPermille / 1_000));

        return new StoreProductStrategyPlan(
            product.Id,
            salePrice,
            priceMultiplier,
            reorderPoint,
            targetQuantity,
            RegionalDistributorId,
            UseEmergencySupplierWhenOutOfStock: true);
    }

    private static int PriceMultiplier(StorePricingPreset pricing) => pricing switch
    {
        StorePricingPreset.HighTurnover => 9_000,
        StorePricingPreset.Balanced => 10_000,
        StorePricingPreset.HighMargin => 10_800,
        _ => throw new ArgumentOutOfRangeException(nameof(pricing))
    };

    private static (int ReorderPermille, int TargetPermille) StockingThresholds(
        StoreStockingPreset stocking) => stocking switch
        {
            StoreStockingPreset.Lean => (200, 550),
            StoreStockingPreset.Balanced => (300, 750),
            StoreStockingPreset.FullShelves => (450, 900),
            _ => throw new ArgumentOutOfRangeException(nameof(stocking))
        };
}
