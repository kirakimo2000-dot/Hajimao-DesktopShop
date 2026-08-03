using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Simulation.Employees;

namespace HajimaoDesktopShop.Application.Simulation;

public sealed record SimulationSnapshot(
    long GameMinute,
    ShopSnapshot Shop,
    IReadOnlyList<CustomerSnapshot> Customers,
    IReadOnlyList<EmployeeSnapshot> Employees,
    int CheckoutQueueLength,
    int RestockQueueLength,
    int CompletedSales,
    string? LastRestockFailure);
