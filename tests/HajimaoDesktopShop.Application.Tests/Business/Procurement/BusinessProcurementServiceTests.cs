using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Procurement;

public sealed class BusinessProcurementServiceTests
{
    [Theory]
    [InlineData("local-wholesale", 1_250, 1, 0, 125)]
    [InlineData("regional-distributor", 1_000, 6, 30, 100)]
    [InlineData("direct-manufacturer", 850, 24, 120, 85)]
    public void Channels_ExposeLockedCostMinimumAndLeadTime(
        string channelId,
        int costPermille,
        int minimumQuantity,
        int deliveryMinutes,
        long expectedUnitCostCents)
    {
        var service = CreateService();

        var channel = service.GetProcurementSnapshot().Channels.Single(item => item.Id == channelId);

        Assert.Equal(costPermille, channel.CostPermille);
        Assert.Equal(minimumQuantity, channel.MinimumOrderQuantity);
        Assert.Equal(deliveryMinutes, channel.DeliveryMinutes);
        Assert.Equal(
            expectedUnitCostCents,
            service.QuoteProcurementUnitCost("water", channelId).Cents);
    }

    [Fact]
    public void PlaceOrder_ChargesImmediatelyAndDeliversAfterLeadTime()
    {
        var service = CreateService();

        var result = service.PlaceProcurementOrder(
            "corner-store",
            "water",
            "regional-distributor",
            6);

        Assert.Equal(ProcurementOrderStatus.InTransit, result.Order?.Status);
        Assert.Equal(4_400, service.GetSnapshot().CashCents);
        Assert.Equal(0, GetWaterQuantity(service));
        Assert.Equal(30, Assert.Single(service.GetProcurementSnapshot().PendingOrders).RemainingMinutes);

        for (var minute = 0; minute < 29; minute++)
        {
            service.AdvanceProcurementMinute();
        }

        Assert.Equal(0, GetWaterQuantity(service));
        Assert.Equal(1, Assert.Single(service.GetProcurementSnapshot().PendingOrders).RemainingMinutes);

        service.AdvanceProcurementMinute();

        Assert.Equal(6, GetWaterQuantity(service));
        Assert.Empty(service.GetProcurementSnapshot().PendingOrders);
    }

    [Fact]
    public void LocalWholesale_DeliversImmediatelyAtPremiumCost()
    {
        var service = CreateService();

        var result = service.PlaceProcurementOrder(
            "corner-store",
            "water",
            "local-wholesale",
            2);

        Assert.Equal(ProcurementOrderStatus.Delivered, result.Order?.Status);
        Assert.Equal(4_750, service.GetSnapshot().CashCents);
        Assert.Equal(2, GetWaterQuantity(service));
        Assert.Empty(service.GetProcurementSnapshot().PendingOrders);
    }

    [Fact]
    public void PlaceOrder_RejectsBelowMinimumAndCapacityIncludingInbound()
    {
        var service = CreateService();

        var belowMinimum = service.PlaceProcurementOrder(
            "corner-store",
            "water",
            "regional-distributor",
            5);
        var first = service.PlaceProcurementOrder(
            "corner-store",
            "water",
            "regional-distributor",
            6);
        var exceedsReservedCapacity = service.PlaceProcurementOrder(
            "corner-store",
            "water",
            "regional-distributor",
            15);

        Assert.Equal(ProcurementOrderPlacementStatus.BelowMinimum, belowMinimum.Status);
        Assert.Equal(ProcurementOrderPlacementStatus.Success, first.Status);
        Assert.Equal(ProcurementOrderPlacementStatus.CapacityExceeded, exceedsReservedCapacity.Status);
        Assert.Single(service.GetProcurementSnapshot().PendingOrders);
    }

    private static int GetWaterQuantity(BusinessGameService service) =>
        Assert.Single(service.GetSnapshot().Stores).Products.Single(product => product.Id == "water").Quantity;

    private static BusinessGameService CreateService() =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            [new ShopDefinition(new ShopId("corner-store"), "街角店", 1, Money.Zero)],
            new LevelCurve([0, 100]),
            starterShopId: "corner-store",
            openingCashCents: 5_000);
}
