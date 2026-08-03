using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class PlayableDemoAcceptanceTests
{
    [Fact]
    public void NewGame_RestockPriceCustomerCheckoutAndProfit_FormCompleteLoop()
    {
        var game = new ShopGameService(
            [new ProductDefinition("water", "矿泉水", 100, 180, 200, "ambient")],
            openingCashCents: 50_000);
        var simulation = new ShopSimulation(
            game,
            new ScriptedRandomSource(0d),
            customerSpawnChance: 0.5d,
            maxCustomers: 1);

        simulation.QueueRestock("water", 20);
        simulation.AdvanceRealSeconds(2);
        var priceChange = game.ChangePrice("water", 190);
        simulation.AdvanceRealSeconds(6);
        var snapshot = simulation.GetSnapshot();

        Assert.Equal(PriceChangeStatus.Success, priceChange.Status);
        Assert.Equal(1, snapshot.CompletedSales);
        Assert.Equal(19, Assert.Single(snapshot.Shop.Products).Quantity);
        Assert.Equal(190, snapshot.Shop.RevenueCents);
        Assert.Equal(90, snapshot.Shop.GrossProfitCents);
        Assert.Equal(48_190, snapshot.Shop.CashCents);
    }

    [Fact]
    public void ThirtyGameMinutesEquivalent_StaysBoundedAndFinanciallyValid()
    {
        var game = new ShopGameService(
            [new ProductDefinition("water", "矿泉水", 100, 180, 2_000, "ambient")],
            openingCashCents: 500_000);
        game.PurchaseStock("water", 1_000);
        var simulation = new ShopSimulation(
            game,
            new AlwaysSpawnRandomSource(),
            customerSpawnChance: 1d,
            maxCustomers: 6);

        simulation.AdvanceRealSeconds(1_800);
        var snapshot = simulation.GetSnapshot();

        Assert.Equal(1_800, snapshot.GameMinute);
        Assert.InRange(snapshot.Customers.Count, 0, 6);
        Assert.True(snapshot.CompletedSales > 100);
        Assert.True(snapshot.Shop.CashCents >= 0);
        Assert.InRange(Assert.Single(snapshot.Shop.Products).Quantity, 0, 1_000);
    }

    private sealed class AlwaysSpawnRandomSource : IRandomSource
    {
        public double NextDouble() => 0d;

        public int Next(int exclusiveMax) => 0;
    }
}
