namespace HajimaoDesktopShop.Application.Catalog;

public enum ProductEffectKind
{
    None,
    Splash,
    Slow,
    BonusDrop
}

public sealed record ProductCombatDefinition(
    string ProductId,
    int BasePower,
    int AttackIntervalTicks,
    int RevenueModifierPermille,
    ProductEffectKind Effect,
    int EffectStrengthPermille,
    string[] Tags,
    int DropWeight);
