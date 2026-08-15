namespace HajimaoDesktopShop.Domain.Collections;

public sealed record StoreProductLoadout
{
    public StoreProductLoadout(
        string storeId,
        int unlockedSlots,
        IReadOnlyList<string> productIds)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (unlockedSlots is < 3 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(unlockedSlots));
        }

        ArgumentNullException.ThrowIfNull(productIds);
        if (productIds.Count > unlockedSlots
            || productIds.Any(string.IsNullOrWhiteSpace)
            || productIds.Distinct(StringComparer.Ordinal).Count() != productIds.Count)
        {
            throw new ArgumentException("Loadout products must be unique and fit unlocked slots.", nameof(productIds));
        }

        StoreId = storeId.Trim();
        UnlockedSlots = unlockedSlots;
        ProductIds = Array.AsReadOnly(productIds.ToArray());
    }

    public string StoreId { get; }
    public int UnlockedSlots { get; }
    public IReadOnlyList<string> ProductIds { get; }

    public StoreProductLoadout WithEquipped(int slotIndex, string productId)
    {
        if (slotIndex < 0 || slotIndex >= UnlockedSlots || slotIndex > ProductIds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        var products = ProductIds.ToList();
        if (slotIndex == products.Count)
        {
            products.Add(productId);
        }
        else
        {
            products[slotIndex] = productId;
        }

        return new StoreProductLoadout(StoreId, UnlockedSlots, products);
    }
}
