using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class ShopSimulationRestockerTests
{
    [Fact]
    public void QueueRestock_DoesNotChangeInventoryUntilRestockerCompletesTask()
    {
        var simulation = CreateSimulation(openingCashCents: 10_000);

        simulation.QueueRestock("water", 3);
        var queued = simulation.GetSnapshot();

        Assert.Equal(0, GetProductQuantity(queued, "water"));
        Assert.Equal(1, queued.RestockQueueLength);

        simulation.AdvanceRealSecond();
        var working = simulation.GetSnapshot();
        Assert.Equal(0, GetProductQuantity(working, "water"));
        Assert.Equal(
            EmployeeState.Working,
            Assert.Single(working.Employees, employee => employee.Role == EmployeeRole.Restocker).State);

        simulation.AdvanceRealSecond();
        var completed = simulation.GetSnapshot();
        Assert.Equal(3, GetProductQuantity(completed, "water"));
        Assert.Equal(9_700, completed.Shop.CashCents);
        Assert.Equal(0, completed.RestockQueueLength);
        Assert.Null(completed.LastRestockFailure);
        Assert.Equal(
            EmployeeState.Idle,
            Assert.Single(completed.Employees, employee => employee.Role == EmployeeRole.Restocker).State);
    }

    [Fact]
    public void RestockTasks_AreFirstInFirstOut()
    {
        var simulation = CreateSimulation(openingCashCents: 10_000);
        simulation.QueueRestock("water", 2);
        simulation.QueueRestock("milk", 1);

        Advance(simulation, 2);
        var firstCompleted = simulation.GetSnapshot();
        Assert.Equal(2, GetProductQuantity(firstCompleted, "water"));
        Assert.Equal(0, GetProductQuantity(firstCompleted, "milk"));

        Advance(simulation, 2);
        var bothCompleted = simulation.GetSnapshot();
        Assert.Equal(2, GetProductQuantity(bothCompleted, "water"));
        Assert.Equal(1, GetProductQuantity(bothCompleted, "milk"));
    }

    [Fact]
    public void Restock_WithInsufficientCash_ReportsFailureAndReturnsIdle()
    {
        var simulation = CreateSimulation(openingCashCents: 100);
        simulation.QueueRestock("water", 2);

        Advance(simulation, 2);
        var snapshot = simulation.GetSnapshot();

        Assert.Equal(0, GetProductQuantity(snapshot, "water"));
        Assert.Equal("water:InsufficientFunds", snapshot.LastRestockFailure);
        Assert.Equal(
            EmployeeState.Idle,
            Assert.Single(snapshot.Employees, employee => employee.Role == EmployeeRole.Restocker).State);
    }

    private static ShopSimulation CreateSimulation(long openingCashCents)
    {
        var game = new ShopGameService(
            [
                new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient"),
                new ProductDefinition("milk", "鲜牛奶", 300, 480, 12, "chilled")
            ],
            openingCashCents);

        return new ShopSimulation(
            game,
            new ScriptedRandomSource(),
            customerSpawnChance: 0d);
    }

    private static int GetProductQuantity(SimulationSnapshot snapshot, string productId) =>
        Assert.Single(snapshot.Shop.Products, product => product.Id == productId).Quantity;

    private static void Advance(ShopSimulation simulation, int seconds)
    {
        for (var second = 0; second < seconds; second++)
        {
            simulation.AdvanceRealSecond();
        }
    }
}
