namespace HajimaoDesktopShop.Domain.Combat;

public enum ProductCombatEffectKind
{
    None,
    Splash,
    Slow,
    BonusDrop
}

public sealed record ProductCombatStats(
    string ProductId,
    int BasePower,
    int AttackIntervalTicks,
    IReadOnlyList<string> Tags,
    ProductCombatEffectKind Effect,
    int EffectStrengthPermille);

public sealed record CharacterCombatStats(
    int BaseAttackIntervalTicks,
    int ProjectileTravelTicks);

public sealed record CustomerSpawnRequest(
    string ArchetypeId,
    int DemandHp,
    int MovementPermillePerTick,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, int> ResistancePermille);
