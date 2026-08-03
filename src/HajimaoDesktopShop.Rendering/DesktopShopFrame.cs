using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Rendering;

public sealed record DesktopShopFrame(
    SimulationSnapshot Snapshot,
    string CashText,
    string GameTimeText,
    string StockWarningText,
    string CustomerCountText,
    bool IsLocked,
    bool IsClickThrough);
