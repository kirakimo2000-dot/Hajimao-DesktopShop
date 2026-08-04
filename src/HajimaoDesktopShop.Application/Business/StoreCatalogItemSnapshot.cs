namespace HajimaoDesktopShop.Application.Business;

public sealed record StoreCatalogItemSnapshot(
    string Id,
    string Name,
    int RequiredPlayerLevel,
    long OpeningCostCents,
    bool IsOpen);
