namespace HajimaoDesktopShop.Application.Catalog;

public sealed record CharacterDefinition(
    string Id,
    string RigId,
    string SkinId,
    int BaseAttackIntervalTicks,
    int ProjectileTravelTicks = 6);
