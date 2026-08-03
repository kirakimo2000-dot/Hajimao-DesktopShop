namespace HajimaoDesktopShop.Application.Game;

public sealed record ProductSnapshot(
    string Id,
    string Name,
    long WholesalePriceCents,
    long SalePriceCents,
    int Quantity,
    int Capacity,
    string ShelfKind);
