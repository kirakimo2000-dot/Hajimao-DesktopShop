namespace HajimaoDesktopShop.Domain.Combat;

public abstract record CombatEvent;

public sealed record CustomerSpawnedEvent(
    long CustomerEntityId,
    string ArchetypeId) : CombatEvent;

public sealed record ProductThrownEvent(
    long ProjectileEntityId,
    string ProductId,
    long TargetCustomerEntityId) : CombatEvent;

public sealed record ProductHitEvent(
    long ProjectileEntityId,
    string ProductId,
    long CustomerEntityId,
    int Damage,
    int RemainingDemandHp,
    bool IsSplash,
    int TargetPositionPermille = 5_000) : CombatEvent;

public sealed record CustomerServedEvent(
    long CustomerEntityId,
    string ArchetypeId) : CombatEvent;

public sealed record CustomerEscapedEvent(
    long CustomerEntityId,
    string ArchetypeId) : CombatEvent;
