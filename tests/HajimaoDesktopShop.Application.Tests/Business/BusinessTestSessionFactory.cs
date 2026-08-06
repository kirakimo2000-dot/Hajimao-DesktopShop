using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business;

internal static class BusinessTestSessionFactory
{
    public static BusinessSession Create(
        bool openSecondStore = false,
        long openingCashCents = 100_000,
        ulong randomState = 123)
    {
        var session = BusinessSession.Create(
            Products(),
            Stores(),
            new LevelCurve([0, 100]),
            "store-1",
            openingCashCents,
            Assignments(),
            new StatefulTestRandomSource(randomState),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 4_000));

        if (openSecondStore)
        {
            var result = session.Game.OpenStore("store-2");
            Assert.Equal(OpenShopStatus.Success, result.Status);
        }

        return session;
    }

    public static BusinessSession Restore(GameSaveData save, ulong randomState = 1) =>
        BusinessSession.RestoreOrUpgrade(
            Products(),
            Stores(),
            new LevelCurve([0, 100]),
            "store-1",
            save,
            Assignments(),
            new StatefulTestRandomSource(randomState),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 4_000));

    private static ProductDefinition[] Products() =>
    [
        new("water", "矿泉水", 100, 200, 100, "ambient"),
        new("bread", "面包", 120, 240, 80, "ambient")
    ];

    private static ShopDefinition[] Stores() =>
    [
        new(new ShopId("store-1"), "街角店", 1, Money.Zero),
        new(new ShopId("store-2"), "社区店", 1, Money.Zero)
    ];

    private static StoreEmployeeAssignment[] Assignments() =>
    [
        Assignment("store-1", "store-1-cashier", EmployeeRole.Cashier),
        Assignment("store-1", "store-1-restocker", EmployeeRole.Restocker),
        Assignment("store-2", "store-2-cashier", EmployeeRole.Cashier),
        Assignment("store-2", "store-2-restocker", EmployeeRole.Restocker)
    ];

    private static StoreEmployeeAssignment Assignment(
        string storeId,
        string employeeId,
        EmployeeRole role) =>
        new(
            storeId,
            new Employee(
                new EmployeeId(employeeId),
                employeeId,
                role,
                1_000,
                new Money(600)));
}
