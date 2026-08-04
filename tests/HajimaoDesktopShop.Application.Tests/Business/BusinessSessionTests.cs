using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business;

public sealed class BusinessSessionTests
{
    private static readonly DateTimeOffset SavedAt =
        new(2026, 8, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CaptureAndRestore_RoundTripsCompleteV5SessionAndCompatibilityProjection()
    {
        var session = CreateSession();
        session.Game.PurchaseStock("corner-store", "water", 20);
        session.Game.ChangePrice("corner-store", "water", 245);
        session.Game.ConfigureAutoRestock(new AutoRestockPolicy(
            "corner-store",
            "water",
            IsEnabled: true,
            ReorderPoint: 20,
            TargetQuantity: 50,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: true));
        session.Game.PlaceProcurementOrder(
            "corner-store",
            "water",
            "regional-distributor",
            6);
        session.Simulation.AdvanceRealSeconds(90);

        var save = session.CaptureSaveData(SavedAt);
        var restored = RestoreSession(save);

        Assert.Equal(GameSaveSchema.CurrentVersion, save.SchemaVersion);
        Assert.NotNull(save.Business);
        Assert.NotNull(save.Business.Procurement);
        Assert.NotNull(save.BusinessSimulation);
        Assert.Equal(save.Business.CashCents, save.Shop.CashCents);
        Assert.Equal(save.BusinessSimulation.GameMinute, save.Simulation.GameMinute);
        Assert.Equivalent(session.Simulation.GetSnapshot(), restored.Simulation.GetSnapshot(), strict: true);
        Assert.Equivalent(
            session.Game.GetProcurementSnapshot(),
            restored.Game.GetProcurementSnapshot(),
            strict: true);
        Assert.Equivalent(save, restored.CaptureSaveData(SavedAt), strict: true);
    }

    [Fact]
    public void RestoreOrUpgrade_CompatibilityOnlyV5StatePreservesStarterStoreAndClock()
    {
        var legacy = new GameSaveData(
            GameSaveSchema.CurrentVersion,
            SavedAt,
            new ShopSaveData(
                12_345,
                4_000,
                2_000,
                2_000,
                [new ProductSaveData("water", 275, 7)]),
            new SimulationSaveData(88, 88, 2, 3, [], [], null, [], null, null));

        var restored = RestoreSession(legacy);
        var snapshot = restored.Simulation.GetSnapshot();

        Assert.Equal(88, snapshot.GameMinute);
        Assert.Equal(12_345, snapshot.Business.CashCents);
        var water = Assert.Single(Assert.Single(snapshot.Business.Stores).Products);
        Assert.Equal(275, water.SalePriceCents);
        Assert.Equal(7, water.Quantity);
    }

    private static BusinessSession CreateSession() =>
        BusinessSession.Create(
            Products(),
            Stores(),
            new LevelCurve([0, 100]),
            "corner-store",
            100_000,
            [CashierAssignment()],
            new StatefulTestRandomSource(123),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 4_000));

    private static BusinessSession RestoreSession(GameSaveData save) =>
        BusinessSession.RestoreOrUpgrade(
            Products(),
            Stores(),
            new LevelCurve([0, 100]),
            "corner-store",
            save,
            [CashierAssignment()],
            new StatefulTestRandomSource(1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 4_000));

    private static ProductDefinition[] Products() =>
    [
        new("water", "矿泉水", 100, 200, 100, "ambient")
    ];

    private static ShopDefinition[] Stores() =>
    [
        new(new ShopId("corner-store"), "街角店", 1, Money.Zero),
        new(new ShopId("station-store"), "车站店", 2, new Money(30_000))
    ];

    private static StoreEmployeeAssignment CashierAssignment() =>
        new(
            "corner-store",
            new Employee(
                new EmployeeId("cashier"),
                "小葵",
                EmployeeRole.Cashier,
                1_000,
                new Money(1_001)));
}
