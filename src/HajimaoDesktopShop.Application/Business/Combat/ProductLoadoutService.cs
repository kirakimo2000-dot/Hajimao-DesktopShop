using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed class ProductLoadoutService
{
    public StoreProductLoadout Equip(
        StoreProductLoadout loadout,
        ProductCollection collection,
        int slotIndex,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(collection);
        if (!collection.IsUnlocked(productId))
        {
            throw new InvalidOperationException($"Product '{productId}' is not unlocked.");
        }

        return loadout.WithEquipped(slotIndex, productId);
    }
}
