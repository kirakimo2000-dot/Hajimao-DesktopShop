using HajimaoDesktopShop.Application.Business;

namespace HajimaoDesktopShop.Desktop.Services;

public sealed record DesktopBusinessSessionStartResult(
    BusinessSession Session,
    bool IsNewGame);
