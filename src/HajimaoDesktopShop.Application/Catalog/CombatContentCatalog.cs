namespace HajimaoDesktopShop.Application.Catalog;

public sealed record CombatContentCatalog(
    IReadOnlyList<ProductCombatDefinition> Products,
    IReadOnlyList<CustomerArchetypeDefinition> Customers,
    IReadOnlyList<CustomerSpawnPoolDefinition> SpawnPools,
    IReadOnlyList<CustomerSpawnEventModifierDefinition> EventModifiers,
    IReadOnlyList<CharacterDefinition> Characters,
    IReadOnlyList<StoreInteriorDefinition> Interiors);
