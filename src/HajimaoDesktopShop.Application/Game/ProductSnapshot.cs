namespace HajimaoDesktopShop.Application.Game;

public sealed record ProductSnapshot(
    string Id,
    string Name,
    long WholesalePriceCents,
    long SalePriceCents,
    int Quantity,
    int Capacity,
    string ShelfKind,
    int RequiredPlayerLevel = 1,
    long UnitGrossProfitCents = 0,
    int GrossMarginBasisPoints = 0,
    long ReferenceSalePriceCents = 0,
    int DemandWeightPermille = 1_000);
