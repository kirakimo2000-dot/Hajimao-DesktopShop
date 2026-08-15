using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed record StoreCombatSnapshot(
    string StoreId,
    StoreCombatState State,
    IReadOnlyList<CombatEvent> Events,
    IReadOnlyList<ProductDropRoll> DropRolls,
    long RevenueCents,
    int ServedCustomers,
    int EscapedCustomers,
    int DroppedProducts,
    string StoreFormatId = "legacy",
    StoreCombatProfile? Profile = null,
    int EncounteredCustomers = 0,
    long TotalDamage = 0);

public sealed record BusinessCombatSnapshot(
    long CashCents,
    IReadOnlyList<StoreCombatSnapshot> Stores,
    IReadOnlyList<ProductCollectionEntry> Collection,
    IReadOnlyList<StoreProductLoadout> Loadouts,
    IReadOnlyList<string>? ActiveEventTags = null);

public sealed record BusinessCombatOptions
{
    public BusinessCombatOptions(
        int spawnChanceBasisPoints = 1_200,
        int maxActiveCustomersPerStore = 4)
    {
        if (spawnChanceBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnChanceBasisPoints));
        }

        if (maxActiveCustomersPerStore <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxActiveCustomersPerStore));
        }

        SpawnChanceBasisPoints = spawnChanceBasisPoints;
        MaxActiveCustomersPerStore = maxActiveCustomersPerStore;
    }

    public int SpawnChanceBasisPoints { get; }
    public int MaxActiveCustomersPerStore { get; }
}
