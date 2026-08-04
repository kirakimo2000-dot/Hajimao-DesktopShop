namespace HajimaoDesktopShop.Rendering;

public sealed record BusinessShopFrame(
    BusinessShopSceneFrame Scene,
    string CashText,
    string PlayerLevelText,
    string GameTimeText,
    string StockWarningText,
    string CustomerCountText,
    bool IsLocked,
    bool IsClickThrough);
