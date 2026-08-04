using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Offline;

namespace HajimaoDesktopShop.Desktop.Services;

public sealed record DesktopBusinessSessionStartResult(
    BusinessSession Session,
    bool IsNewGame,
    OfflineSettlementResult? OfflineSettlement);
