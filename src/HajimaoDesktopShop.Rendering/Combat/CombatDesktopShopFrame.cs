namespace HajimaoDesktopShop.Rendering.Combat;

public sealed record CombatDesktopShopFrame(
    CombatShopSceneFrame Scene,
    string CashText,
    string PlayerLevelText,
    string IncomeText,
    string CustomerCountText,
    bool IsLocked,
    bool IsClickThrough);
