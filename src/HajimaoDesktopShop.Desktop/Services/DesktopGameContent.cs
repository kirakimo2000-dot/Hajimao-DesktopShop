using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopGameContent
{
    public const string StarterStoreId = "corner-store";

    public static IReadOnlyList<ShopDefinition> Shops { get; } = Array.AsReadOnly(
    new[]
    {
        new ShopDefinition(new ShopId(StarterStoreId), "街角便利店", 1, Money.Zero),
        new ShopDefinition(new ShopId("station-store"), "车站便利店", 3, new Money(80_000)),
        new ShopDefinition(new ShopId("community-store"), "社区生活店", 5, new Money(200_000))
    });

    public static LevelCurve LevelCurve { get; } =
        new([0, 40, 120, 300, 650, 1_200, 2_000, 3_200, 5_000, 7_500]);

    public static IReadOnlyList<StoreEmployeeAssignment> CreateStarterAssignments() =>
        Array.AsReadOnly(new[]
        {
            new StoreEmployeeAssignment(
                StarterStoreId,
                new Employee(
                    new EmployeeId("starter-cashier"),
                    "小葵",
                    EmployeeRole.Cashier,
                    1_000,
                    new Money(6_000))),
            new StoreEmployeeAssignment(
                StarterStoreId,
                new Employee(
                    new EmployeeId("starter-restocker"),
                    "阿澄",
                    EmployeeRole.Restocker,
                    950,
                    new Money(5_400)))
        });
}
