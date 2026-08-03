using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Simulation.Employees;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class ShopSimulationCustomerTests
{
    [Fact]
    public void AdvanceRealSecond_CustomerCompletesPurchaseThroughCashier()
    {
        var game = CreateGame();
        game.PurchaseStock("water", 5);
        var simulation = new ShopSimulation(
            game,
            new ScriptedRandomSource(0d),
            customerSpawnChance: 0.5d,
            maxCustomers: 1);

        simulation.AdvanceRealSecond();
        Assert.Equal(CustomerState.Entering, Assert.Single(simulation.GetSnapshot().Customers).State);

        simulation.AdvanceRealSecond();
        Assert.Equal(CustomerState.SeekingProduct, Assert.Single(simulation.GetSnapshot().Customers).State);

        simulation.AdvanceRealSecond();
        Assert.Equal(CustomerState.Queueing, Assert.Single(simulation.GetSnapshot().Customers).State);

        simulation.AdvanceRealSecond();
        var checkoutSnapshot = simulation.GetSnapshot();
        Assert.Equal(CustomerState.CheckingOut, Assert.Single(checkoutSnapshot.Customers).State);
        Assert.Equal(
            EmployeeState.Working,
            Assert.Single(checkoutSnapshot.Employees, employee => employee.Role == EmployeeRole.Cashier).State);

        simulation.AdvanceRealSecond();
        Assert.Equal(CustomerState.Leaving, Assert.Single(simulation.GetSnapshot().Customers).State);

        simulation.AdvanceRealSecond();
        var snapshot = simulation.GetSnapshot();
        var water = Assert.Single(snapshot.Shop.Products);

        Assert.Empty(snapshot.Customers);
        Assert.Equal(1, snapshot.CompletedSales);
        Assert.Equal(4, water.Quantity);
        Assert.Equal(9_700, snapshot.Shop.CashCents);
    }

    [Fact]
    public void AdvanceRealSecond_WithNoStock_CustomerLeavesWithoutSale()
    {
        var simulation = new ShopSimulation(
            CreateGame(),
            new ScriptedRandomSource(0d),
            customerSpawnChance: 0.5d,
            maxCustomers: 1);

        Advance(simulation, 3);
        Assert.Equal(CustomerState.Leaving, Assert.Single(simulation.GetSnapshot().Customers).State);

        simulation.AdvanceRealSecond();
        var snapshot = simulation.GetSnapshot();

        Assert.Empty(snapshot.Customers);
        Assert.Equal(0, snapshot.CompletedSales);
        Assert.Equal(10_000, snapshot.Shop.CashCents);
    }

    [Fact]
    public void CheckoutQueue_IsFirstInFirstOut()
    {
        var game = CreateGame();
        game.PurchaseStock("water", 5);
        var simulation = new ShopSimulation(
            game,
            new ScriptedRandomSource(0d, 0d),
            customerSpawnChance: 0.5d,
            maxCustomers: 2);

        Advance(simulation, 4);
        var customers = simulation.GetSnapshot().Customers.OrderBy(customer => customer.Id).ToArray();

        Assert.Equal(CustomerState.CheckingOut, customers[0].State);
        Assert.Equal(CustomerState.Queueing, customers[1].State);
    }

    private static ShopGameService CreateGame() =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            openingCashCents: 10_000);

    private static void Advance(ShopSimulation simulation, int seconds)
    {
        for (var second = 0; second < seconds; second++)
        {
            simulation.AdvanceRealSecond();
        }
    }
}
