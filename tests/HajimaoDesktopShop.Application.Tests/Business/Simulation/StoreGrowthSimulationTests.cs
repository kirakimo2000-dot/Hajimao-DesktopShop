using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

public sealed class StoreGrowthSimulationTests
{
    [Fact]
    public void ExpansionQueueComfort_ReducesOnlyQueuePenaltyAndAddsAttraction()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 10);
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(Enumerable.Repeat(0d, 32).ToArray()),
            new BusinessSimulationOptions(10_000, 10_000));
        simulation.AdvanceRealSeconds(3);
        var before = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand;

        service.UpgradeStore("corner-store", StoreUpgradeKind.Expansion);
        var after = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand;

        Assert.Equal(before.PriceAdjustmentBasisPoints, after.PriceAdjustmentBasisPoints);
        Assert.True(after.QueueAdjustmentBasisPoints > before.QueueAdjustmentBasisPoints);
        Assert.True(after.AttractionAdjustmentBasisPoints > before.AttractionAdjustmentBasisPoints);
    }

    [Fact]
    public void Promotion_AppliesForCurrentMinuteThenExpiresAtTickEnd()
    {
        var service = CreateService();
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(Enumerable.Repeat(1d, 300).ToArray()));
        service.StartPromotion("corner-store", "local-flyers");

        var initial = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand;
        simulation.AdvanceRealSeconds(239);
        var finalActive = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand;
        simulation.AdvanceRealSeconds(1);
        var expired = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand;

        Assert.Equal(1_200, initial.PromotionAdjustmentBasisPoints);
        Assert.Equal(1_200, finalActive.PromotionAdjustmentBasisPoints);
        Assert.Equal(0, expired.PromotionAdjustmentBasisPoints);
        Assert.Null(service.GetStoreGrowthSnapshot("corner-store").ActivePromotion);
    }

    [Fact]
    public void DayReport_SubtractsDevelopmentAndPromotionOperatingCost()
    {
        var service = CreateService();
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(Enumerable.Repeat(1d, 1_500).ToArray()));
        service.UpgradeStore("corner-store", StoreUpgradeKind.Shelf);
        service.StartPromotion("corner-store", "local-flyers");

        simulation.AdvanceRealSeconds(1_440);
        var report = Assert.Single(simulation.GetSnapshot().LastCompletedDay!.Stores);

        Assert.Equal(40_000, report.OperatingCostCents);
        Assert.Equal(
            report.GrossProfitCents - report.WageCostCents - report.OperatingCostCents,
            report.NetProfitCents);
    }

    private static BusinessGameService CreateService() =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient", 1)],
            [new ShopDefinition(new ShopId("corner-store"), "街角便利店", 1, Money.Zero)],
            new LevelCurve([0, 10]),
            starterShopId: "corner-store",
            openingCashCents: 1_000_000);
}
