namespace HajimaoDesktopShop.Application.Simulation.Customers;

public sealed record CustomerSnapshot(
    long Id,
    CustomerState State,
    string? SelectedProductId);
