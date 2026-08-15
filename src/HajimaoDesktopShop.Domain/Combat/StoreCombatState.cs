namespace HajimaoDesktopShop.Domain.Combat;

public sealed record ActiveCustomerState(
    long EntityId,
    string ArchetypeId,
    int DemandHp,
    int PositionPermille,
    int MovementPermillePerTick,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, int> ResistancePermille,
    int SlowStrengthPermille,
    int SlowTicksRemaining,
    int MaximumDemandHp = 0);

public sealed record ProductProjectileState(
    long EntityId,
    string ProductId,
    long TargetCustomerEntityId,
    int RemainingTravelTicks,
    int Power,
    IReadOnlyList<string> Tags,
    ProductCombatEffectKind Effect,
    int EffectStrengthPermille,
    int TotalTravelTicks = 0);

public sealed record StoreCombatState(
    long NextEntityId,
    int AttackCooldownTicks,
    int NextProductIndex,
    IReadOnlyList<ActiveCustomerState> Customers,
    IReadOnlyList<ProductProjectileState> Projectiles)
{
    public static StoreCombatState Empty { get; } = new(1, 0, 0, [], []);
}
