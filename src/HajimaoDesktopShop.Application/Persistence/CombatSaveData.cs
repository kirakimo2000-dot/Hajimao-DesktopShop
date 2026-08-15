using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Application.Persistence;

public sealed record ProductCollectionSaveData(
    IReadOnlyList<ProductCollectionEntry> Entries);

public sealed record StoreProductLoadoutSaveData(
    string StoreId,
    int UnlockedSlots,
    IReadOnlyList<string> ProductIds);

public sealed record StoreCombatStateSaveData(
    string StoreId,
    StoreCombatState State,
    long RevenueCents = 0,
    int ServedCustomers = 0,
    int EscapedCustomers = 0,
    int DroppedProducts = 0,
    int EncounteredCustomers = 0,
    long TotalDamage = 0);

public sealed record LegacyEmployeeArchiveEntry(
    string StoreId,
    string EmployeeId,
    string Name,
    string CharacterId);

public sealed record LegacyCombatCompatibilitySaveData(
    IReadOnlyList<LegacyEmployeeArchiveEntry> Employees);

public sealed record CombatSaveData(
    ProductCollectionSaveData Collection,
    IReadOnlyList<StoreProductLoadoutSaveData> Loadouts,
    IReadOnlyList<StoreCombatStateSaveData> Stores,
    ulong RandomState,
    LegacyCombatCompatibilitySaveData Compatibility);
