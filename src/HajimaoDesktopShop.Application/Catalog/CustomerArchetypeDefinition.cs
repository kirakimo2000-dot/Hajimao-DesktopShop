namespace HajimaoDesktopShop.Application.Catalog;

public sealed record CustomerArchetypeDefinition(
    string Id,
    int DemandHp,
    int MovementPermillePerTick,
    long BaseRewardCents,
    string[] Tags,
    IReadOnlyDictionary<string, int> ResistancePermille,
    IReadOnlyDictionary<string, int> ProductDropWeights);

public sealed record CustomerSpawnPoolEntry(string CustomerId, int Weight);

public sealed record CustomerSpawnPoolDefinition(
    string Id,
    int StartHourInclusive,
    int EndHourExclusive,
    IReadOnlyList<CustomerSpawnPoolEntry> Entries);

public sealed record CustomerSpawnEventModifierDefinition(
    string EventTag,
    string CustomerTag,
    int WeightModifierPermille,
    int AddedWeight);
