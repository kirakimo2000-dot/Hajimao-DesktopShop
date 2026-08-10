using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Procurement;

public sealed class AutoRestockTests
{
    [Fact]
    public void DefaultPolicies_WaitForNormalProcurementMinuteBeforeOrdering()
    {
        var game = BusinessTestSessionFactory.Create().Game;

        Assert.Empty(game.GetProcurementSnapshot().PendingOrders);

        game.AdvanceProcurementMinute();

        Assert.NotEmpty(game.GetProcurementSnapshot().PendingOrders);
        Assert.All(
            game.GetProcurementSnapshot().PendingOrders,
            order => Assert.True(order.IsAutomatic));
    }

    [Fact]
    public void SimulationTick_OrdersToTargetUsingOnHandPlusInbound()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 2);
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "corner-store",
            "water",
            IsEnabled: true,
            ReorderPoint: 2,
            TargetQuantity: 12,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: false));
        var simulation = CreateQuietSimulation(service);

        simulation.AdvanceRealSecond();

        var order = Assert.Single(service.GetProcurementSnapshot().PendingOrders);
        Assert.Equal(10, order.Quantity);
        Assert.True(order.IsAutomatic);
        Assert.Equal(30, order.RemainingMinutes);
    }

    [Fact]
    public void AdvanceMinute_DoesNotDuplicateAutomaticInboundOrder()
    {
        var service = CreateService();
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "corner-store",
            "water",
            IsEnabled: true,
            ReorderPoint: 6,
            TargetQuantity: 12,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: false));

        service.AdvanceProcurementMinute();
        service.AdvanceProcurementMinute();

        var order = Assert.Single(service.GetProcurementSnapshot().PendingOrders);
        Assert.Equal(12, order.Quantity);
        Assert.Equal(29, order.RemainingMinutes);
    }

    [Fact]
    public void OutOfStock_UsesLocalEmergencyOrderWhenPreferredOrderCannotBePaid()
    {
        var service = CreateService(openingCashCents: 130);
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "corner-store",
            "water",
            IsEnabled: true,
            ReorderPoint: 0,
            TargetQuantity: 6,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: true));

        service.AdvanceProcurementMinute();

        Assert.Equal(1, GetWaterQuantity(service));
        Assert.Equal(5, service.GetSnapshot().CashCents);
        Assert.Empty(service.GetProcurementSnapshot().PendingOrders);
    }

    private static BusinessSimulation CreateQuietSimulation(BusinessGameService service) =>
        new(
            service,
            [],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

    private static int GetWaterQuantity(BusinessGameService service) =>
        Assert.Single(service.GetSnapshot().Stores).Products.Single(product => product.Id == "water").Quantity;

    private static BusinessGameService CreateService(long openingCashCents = 5_000) =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            [new ShopDefinition(new ShopId("corner-store"), "街角店", 1, Money.Zero)],
            new LevelCurve([0, 100]),
            starterShopId: "corner-store",
            openingCashCents);
}
