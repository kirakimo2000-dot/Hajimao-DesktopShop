using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Progression;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;

namespace HajimaoDesktopShop.Application.Tests.Business.Progression;

public sealed class LongTermProgressionServiceTests
{
    [Fact]
    public void Create_FreshStoreStartsWithProfitableDayInsteadOfAPlaytimeDeadline()
    {
        var result = LongTermProgressionService.Create(
            Snapshot(playerLevel: 1, cashCents: 50_000, openStores: 1, positiveDay: false),
            Catalog(openStores: 1),
            GrowthLevels(0),
            hasAnyInvestment: false);

        Assert.Equal(ProgressionGoalId.ReachProfitableDay, result.CurrentGoal.Id);
        Assert.Equal("store-1", result.CurrentGoal.TargetStoreId);
        Assert.Equal(1, result.OpenStoreCount);
    }

    [Fact]
    public void Create_ProfitableStoreRequiresInvestmentBeforeExpansionPreparation()
    {
        var result = LongTermProgressionService.Create(
            Snapshot(playerLevel: 2, cashCents: 100_000, openStores: 1),
            Catalog(openStores: 1),
            GrowthLevels(0),
            hasAnyInvestment: false);

        Assert.Equal(ProgressionGoalId.MakeFirstInvestment, result.CurrentGoal.Id);
    }

    [Theory]
    [InlineData(2, 200_000, ProgressionGoalId.OpenSecondStore)]
    [InlineData(3, 79_999, ProgressionGoalId.PrepareSecondStore)]
    [InlineData(3, 80_000, ProgressionGoalId.OpenSecondStore)]
    public void Create_SingleInvestedStoreUsesCashRatherThanLevelForReadiness(
        int level,
        long cashCents,
        ProgressionGoalId expected)
    {
        var result = LongTermProgressionService.Create(
            Snapshot(level, cashCents, openStores: 1),
            Catalog(openStores: 1),
            GrowthLevels(1),
            hasAnyInvestment: true);

        Assert.Equal(expected, result.CurrentGoal.Id);
        Assert.Equal("store-2", result.CurrentGoal.TargetStoreId);
        Assert.Equal(0, result.CurrentGoal.RequiredPlayerLevel);
        Assert.Equal(80_000, result.CurrentGoal.RequiredCashCents);
    }

    [Fact]
    public void Create_TwoStoresStrengthensTheWeakestBeforePreparingThirdStore()
    {
        var result = LongTermProgressionService.Create(
            Snapshot(playerLevel: 5, cashCents: 300_000, openStores: 2),
            Catalog(openStores: 2),
            GrowthLevels(3, 1),
            hasAnyInvestment: true);

        Assert.Equal(ProgressionGoalId.StrengthenPortfolio, result.CurrentGoal.Id);
        Assert.Equal("store-2", result.CurrentGoal.TargetStoreId);
        Assert.Equal(1, result.CurrentGoal.CurrentValue);
        Assert.Equal(2, result.CurrentGoal.TargetValue);
    }

    [Theory]
    [InlineData(4, 300_000, ProgressionGoalId.OpenThirdStore)]
    [InlineData(5, 199_999, ProgressionGoalId.PrepareThirdStore)]
    [InlineData(5, 200_000, ProgressionGoalId.OpenThirdStore)]
    public void Create_DevelopedTwoStorePortfolioUsesCashForThirdStoreReadiness(
        int level,
        long cashCents,
        ProgressionGoalId expected)
    {
        var result = LongTermProgressionService.Create(
            Snapshot(level, cashCents, openStores: 2),
            Catalog(openStores: 2),
            GrowthLevels(2, 2),
            hasAnyInvestment: true);

        Assert.Equal(expected, result.CurrentGoal.Id);
        Assert.Equal("store-3", result.CurrentGoal.TargetStoreId);
        Assert.Equal(0, result.CurrentGoal.RequiredPlayerLevel);
        Assert.Equal(200_000, result.CurrentGoal.RequiredCashCents);
    }

    [Fact]
    public void Create_AllConfiguredStoresOpenKeepsImprovingWeakestStoreWithoutLevelGate()
    {
        var beforeStreet = LongTermProgressionService.Create(
            Snapshot(playerLevel: 9, cashCents: 500_000, openStores: 3),
            Catalog(openStores: 3),
            GrowthLevels(4, 2, 3),
            hasAnyInvestment: true);
        var fullStreet = LongTermProgressionService.Create(
            Snapshot(playerLevel: 10, cashCents: 500_000, openStores: 3),
            Catalog(openStores: 3),
            GrowthLevels(4, 2, 3),
            hasAnyInvestment: true);

        Assert.Equal(ProgressionGoalId.ImproveWeakestStore, beforeStreet.CurrentGoal.Id);
        Assert.Equal("store-2", beforeStreet.CurrentGoal.TargetStoreId);
        Assert.Equal(ProgressionGoalId.ImproveWeakestStore, fullStreet.CurrentGoal.Id);
        Assert.Equal("store-2", fullStreet.CurrentGoal.TargetStoreId);
        Assert.Equal(2, fullStreet.CurrentGoal.CurrentValue);
        Assert.Equal(3, fullStreet.CurrentGoal.TargetValue);
    }

    [Fact]
    public void Create_RejectsDuplicateCatalogAndMissingOpenStoreGrowth()
    {
        var snapshot = Snapshot(playerLevel: 1, cashCents: 50_000, openStores: 1);
        var duplicateCatalog = new[] { Catalog(1)[0], Catalog(1)[0] };

        Assert.Throws<ArgumentException>(() => LongTermProgressionService.Create(
            snapshot,
            duplicateCatalog,
            GrowthLevels(0),
            hasAnyInvestment: false));
        Assert.Throws<ArgumentException>(() => LongTermProgressionService.Create(
            snapshot,
            Catalog(1),
            [],
            hasAnyInvestment: false));
    }

    private static BusinessSimulationSnapshot Snapshot(
        int playerLevel,
        long cashCents,
        int openStores,
        bool positiveDay = true)
    {
        var initial = BusinessTestSessionFactory.Create(
            openSecondStore: openStores >= 2,
            openingCashCents: cashCents).Simulation.GetSnapshot();
        var stores = initial.Business.Stores.ToList();
        var operations = initial.Stores.ToList();
        if (openStores == 3)
        {
            stores.Add(stores[0] with { Id = "store-3", Name = "街区店" });
            operations.Add(operations[0] with { StoreId = "store-3" });
        }

        var dayStores = stores.Select(store => new StoreDayReport(
            store.Id,
            Visitors: 20,
            AcceptedPurchases: 10,
            CompletedSales: 10,
            LostSales: 2,
            RevenueCents: 20_000,
            GrossProfitCents: 10_000,
            WageCostCents: 3_000,
            NetProfitCents: positiveDay ? 5_000 : -1,
            ClosingCleanlinessPermille: 900,
            AverageQueueLengthBasisPoints: 0,
            OperatingCostCents: 2_000)).ToArray();
        return initial with
        {
            Business = initial.Business with
            {
                PlayerLevel = playerLevel,
                CashCents = cashCents,
                Stores = stores
            },
            Stores = operations,
            LastCompletedDay = new BusinessDayReport(1, dayStores)
        };
    }

    private static StoreCatalogItemSnapshot[] Catalog(int openStores) =>
    [
        new("store-1", "街角店", 1, 0, IsOpen: true),
        new("store-2", "车站店", 3, 80_000, IsOpen: openStores >= 2),
        new("store-3", "街区店", 5, 200_000, IsOpen: openStores >= 3)
    ];

    private static StoreGrowthSnapshot[] GrowthLevels(params int[] totals) =>
        totals.Select((total, index) => Growth($"store-{index + 1}", total)).ToArray();

    private static StoreGrowthSnapshot Growth(string storeId, int total) =>
        new(
            storeId,
            ExpansionLevel: total,
            ShelfLevel: 0,
            DecorationLevel: 0,
            FloorAreaUnits: 1 + total,
            ShelfSlotCount: 3 + (2 * total),
            QueueComfortCapacity: 2 * total,
            InventoryCapacityPermille: 1_000,
            AttractionBonusBasisPoints: 150 * total,
            NextExpansionUpgradeCostCents: 60_000,
            NextShelfUpgradeCostCents: 25_000,
            NextDecorationUpgradeCostCents: 30_000,
            PromotionArrivalBonusBasisPoints: 0,
            PromotionPurchaseBonusBasisPoints: 0,
            ActivePromotion: null);
}
