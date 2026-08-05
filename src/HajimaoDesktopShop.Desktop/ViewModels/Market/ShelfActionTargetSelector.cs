using HajimaoDesktopShop.Application.Game;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public static class ShelfActionTargetSelector
{
    public static ProductSnapshot? Select(
        IEnumerable<ProductSnapshot> products,
        string shelfKind)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentException.ThrowIfNullOrWhiteSpace(shelfKind);

        return products
            .Where(product => string.Equals(
                product.ShelfKind,
                shelfKind,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(product => product.Capacity <= 0
                ? 1m
                : (decimal)product.Quantity / product.Capacity)
            .ThenBy(product => product.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
