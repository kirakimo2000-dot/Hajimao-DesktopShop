using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.StoreGrowth;

public sealed class StoreGrowthServiceTests
{
    [Fact]
    public void StartPromotion_DebitsOnceAndExpiresAfterConfiguredMinutes()
    {
        var game = CreateService(openingCashCents: 100_000);

        var started = game.StartPromotion("corner-store", "local-flyers");
        game.AdvanceStoreGrowthMinutes(239);
        var beforeExpiry = game.GetStoreGrowthSnapshot("corner-store");
        game.AdvanceStoreGrowthMinute();

        Assert.Equal(StoreGrowthCommandStatus.Success, started.Status);
        Assert.Equal(85_000, game.GetSnapshot().CashCents);
        Assert.Equal(1, beforeExpiry.ActivePromotion!.RemainingMinutes);
        Assert.Null(game.GetStoreGrowthSnapshot("corner-store").ActivePromotion);
    }

    [Fact]
    public void StartPromotion_RejectsConcurrentCampaignAtomically()
    {
        var game = CreateService(openingCashCents: 100_000);
        game.StartPromotion("corner-store", "local-flyers");
        var cashBefore = game.GetSnapshot().CashCents;

        var duplicate = game.StartPromotion("corner-store", "local-flyers");

        Assert.Equal(StoreGrowthCommandStatus.PromotionAlreadyActive, duplicate.Status);
        Assert.Equal(cashBefore, game.GetSnapshot().CashCents);
    }

    [Fact]
    public void CouponsAndFestival_RequireStoreDevelopmentLevels()
    {
        var game = CreateService(openingCashCents: 1_000_000);

        var coupons = game.StartPromotion("corner-store", "discount-coupons");
        var festival = game.StartPromotion("corner-store", "festival-event");

        Assert.Equal(StoreGrowthCommandStatus.PrerequisiteNotMet, coupons.Status);
        Assert.Equal(StoreGrowthCommandStatus.PrerequisiteNotMet, festival.Status);
        Assert.Equal(1_000_000, game.GetSnapshot().CashCents);
    }

    [Fact]
    public void UpgradeSnapshot_ExposesNextCostAndDerivedCapacity()
    {
        var game = CreateService(openingCashCents: 1_000_000);

        var result = game.UpgradeStore("corner-store", StoreUpgradeKind.Shelf);
        var growth = game.GetStoreGrowthSnapshot("corner-store");

        Assert.Equal(StoreGrowthCommandStatus.Success, result.Status);
        Assert.Equal(1, growth.ShelfLevel);
        Assert.Equal(1_250, growth.InventoryCapacityPermille);
        Assert.Equal(100_000, growth.NextShelfUpgradeCostCents);
        Assert.Equal(25_000, Assert.Single(game.GetSnapshot().Stores).OperatingCostCents);
    }

    [Fact]
    public void Promotions_ExposeDocumentedModifiersAndPrerequisites()
    {
        var game = CreateService(openingCashCents: 2_000_000);
        game.UpgradeStore("corner-store", StoreUpgradeKind.Expansion);
        game.UpgradeStore("corner-store", StoreUpgradeKind.Expansion);
        game.UpgradeStore("corner-store", StoreUpgradeKind.Decoration);
        game.UpgradeStore("corner-store", StoreUpgradeKind.Decoration);

        var result = game.StartPromotion("corner-store", "festival-event");
        var growth = game.GetStoreGrowthSnapshot("corner-store");

        Assert.Equal(StoreGrowthCommandStatus.Success, result.Status);
        Assert.Equal(1_600, growth.PromotionArrivalBonusBasisPoints);
        Assert.Equal(600, growth.PromotionPurchaseBonusBasisPoints);
        Assert.Equal(480, growth.ActivePromotion!.RemainingMinutes);
        Assert.Equal(50_000, growth.ActivePromotion.CostCents);
    }

    private static BusinessGameService CreateService(long openingCashCents) =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient", 1)],
            [new ShopDefinition(new ShopId("corner-store"), "街角便利店", 1, Money.Zero)],
            new LevelCurve([0, 10]),
            starterShopId: "corner-store",
            openingCashCents: openingCashCents);
}
