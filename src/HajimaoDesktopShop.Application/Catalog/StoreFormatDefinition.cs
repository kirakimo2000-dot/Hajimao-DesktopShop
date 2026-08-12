using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Catalog;

public sealed record StoreFormatDefinition
{
    public StoreFormatDefinition(
        string id,
        string displayName,
        long baseOpeningCostCents,
        long recommendedReserveCents,
        int baseDemandPermille,
        int priceSensitivityPermille,
        int serviceSensitivityPermille,
        int queueSensitivityPermille,
        int cleanlinessSensitivityPermille,
        int inventoryCapacityPermille,
        string timeProfile,
        IReadOnlyDictionary<string, int> productShelfWeights,
        StorePricingPreset recommendedPricing,
        StoreStockingPreset recommendedStocking)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Store format ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Store format name is required.", nameof(displayName));
        }

        if (baseOpeningCostCents < 0 || recommendedReserveCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseOpeningCostCents));
        }

        var multipliers = new[]
        {
            baseDemandPermille,
            priceSensitivityPermille,
            serviceSensitivityPermille,
            queueSensitivityPermille,
            cleanlinessSensitivityPermille,
            inventoryCapacityPermille
        };
        if (multipliers.Any(value => value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(baseDemandPermille));
        }

        if (string.IsNullOrWhiteSpace(timeProfile))
        {
            throw new ArgumentException("Store time profile is required.", nameof(timeProfile));
        }

        ArgumentNullException.ThrowIfNull(productShelfWeights);
        var requiredShelfKinds = new[] { "ambient", "chilled", "frozen" };
        if (productShelfWeights.Count != requiredShelfKinds.Length
            || requiredShelfKinds.Any(kind => !productShelfWeights.TryGetValue(kind, out var weight) || weight <= 0))
        {
            throw new ArgumentException(
                "Store format requires positive ambient, chilled, and frozen weights.",
                nameof(productShelfWeights));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        BaseOpeningCostCents = baseOpeningCostCents;
        RecommendedReserveCents = recommendedReserveCents;
        BaseDemandPermille = baseDemandPermille;
        PriceSensitivityPermille = priceSensitivityPermille;
        ServiceSensitivityPermille = serviceSensitivityPermille;
        QueueSensitivityPermille = queueSensitivityPermille;
        CleanlinessSensitivityPermille = cleanlinessSensitivityPermille;
        InventoryCapacityPermille = inventoryCapacityPermille;
        TimeProfile = timeProfile.Trim();
        ProductShelfWeights = new Dictionary<string, int>(productShelfWeights, StringComparer.Ordinal);
        RecommendedPricing = recommendedPricing;
        RecommendedStocking = recommendedStocking;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public long BaseOpeningCostCents { get; }
    public long RecommendedReserveCents { get; }
    public int BaseDemandPermille { get; }
    public int PriceSensitivityPermille { get; }
    public int ServiceSensitivityPermille { get; }
    public int QueueSensitivityPermille { get; }
    public int CleanlinessSensitivityPermille { get; }
    public int InventoryCapacityPermille { get; }
    public string TimeProfile { get; }
    public IReadOnlyDictionary<string, int> ProductShelfWeights { get; }
    public StorePricingPreset RecommendedPricing { get; }
    public StoreStockingPreset RecommendedStocking { get; }
}
