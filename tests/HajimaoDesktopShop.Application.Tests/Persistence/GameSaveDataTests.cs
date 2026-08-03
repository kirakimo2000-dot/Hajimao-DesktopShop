using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Employees;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Tests.Persistence;

public sealed class GameSaveDataTests
{
    [Fact]
    public void CaptureAndRestore_RoundTripsShopSimulationAndEmployeeWork()
    {
        ProductDefinition[] definitions =
        [
            new("water", "矿泉水", 100, 200, 20, "ambient"),
            new("milk", "鲜牛奶", 300, 480, 12, "chilled")
        ];
        var game = new ShopGameService(definitions, openingCashCents: 10_000);
        game.PurchaseStock("water", 3);
        game.ChangePrice("water", 250);
        var simulation = new ShopSimulation(
            game,
            new ScriptedRandomSource(0d),
            customerSpawnChance: 0.5d,
            maxCustomers: 2);
        simulation.QueueRestock("milk", 2);
        simulation.AdvanceRealSecond();

        var save = simulation.CaptureSaveData(new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));
        var restoredGame = new ShopGameService(definitions, save.Shop);
        var restored = new ShopSimulation(
            restoredGame,
            new ScriptedRandomSource(),
            save.Simulation,
            customerSpawnChance: 0.5d,
            maxCustomers: 2);

        Assert.Equal(4, save.SchemaVersion);
        Assert.Equivalent(simulation.GetSnapshot(), restored.GetSnapshot(), strict: true);
        Assert.Equal(
            EmployeeState.Working,
            Assert.Single(restored.GetSnapshot().Employees, employee => employee.Role == EmployeeRole.Restocker).State);

        restored.AdvanceRealSecond();
        var afterResume = restored.GetSnapshot();
        Assert.Equal(2, afterResume.GameMinute);
        Assert.Equal(2, Assert.Single(afterResume.Shop.Products, product => product.Id == "milk").Quantity);
    }
}
