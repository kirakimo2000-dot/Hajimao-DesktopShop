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
    long TotalDamage = 0,
    bool IsStaffed = true,
    string? AssignedEmployeeInstanceId = null);

public sealed record CombatEmployeeSnapshot(
    string InstanceId,
    string CharacterId,
    string DisplayName,
    string? AssignedStoreId);

public sealed record ProductAssignmentSnapshot(
    string ProductId,
    string? AssignedStoreId);

public sealed record BusinessCombatSnapshot(
    long CashCents,
    IReadOnlyList<StoreCombatSnapshot> Stores,
    IReadOnlyList<ProductCollectionEntry> Collection,
    IReadOnlyList<StoreProductLoadout> Loadouts,
    IReadOnlyList<string>? ActiveEventTags = null,
    IReadOnlyList<CombatEmployeeSnapshot>? Employees = null,
    IReadOnlyList<ProductAssignmentSnapshot>? ProductAssignments = null);

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
