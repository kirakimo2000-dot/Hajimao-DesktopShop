using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Rendering;

public sealed record BusinessShopFrame(
    BusinessSimulationSnapshot Snapshot,
    string StoreId,
    string CashText,
    string PlayerLevelText,
    string GameTimeText,
    string StockWarningText,
    string CustomerCountText,
    bool IsLocked,
    bool IsClickThrough);
