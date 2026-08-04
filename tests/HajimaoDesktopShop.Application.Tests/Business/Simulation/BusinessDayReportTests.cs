using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

public sealed class BusinessDayReportTests
{
    [Fact]
    public void DayClose_ReportsOperationalAndFinancialDeltas()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 100);
        var simulation = new BusinessSimulation(
            service,
            [AssignCashier("corner-store", "cashier")],
            new ScriptedRandomSource(Enumerable.Repeat(0d, 5_000).ToArray()),
            new BusinessSimulationOptions(
                baseArrivalBasisPoints: 10_000,
                basePurchaseBasisPoints: 10_000));

        simulation.AdvanceRealSeconds(1_440);
        var snapshot = simulation.GetSnapshot();
        var report = Assert.IsType<BusinessDayReport>(snapshot.LastCompletedDay);
        var storeReport = Assert.Single(report.Stores);
        var store = Assert.Single(snapshot.Business.Stores);

        Assert.Equal(1, report.DayNumber);
        Assert.Equal("corner-store", storeReport.StoreId);
        Assert.True(storeReport.Visitors > 0);
        Assert.True(storeReport.CompletedSales > 0);
        Assert.Equal(store.RevenueCents, storeReport.RevenueCents);
        Assert.Equal(store.GrossProfitCents, storeReport.GrossProfitCents);
        Assert.Equal(store.WageCostCents, storeReport.WageCostCents);
        Assert.Equal(store.OperatingCostCents, storeReport.OperatingCostCents);
        Assert.Equal(
            storeReport.GrossProfitCents - storeReport.WageCostCents - storeReport.OperatingCostCents,
            storeReport.NetProfitCents);
        Assert.InRange(storeReport.ClosingCleanlinessPermille, 0, 1_000);
        Assert.True(storeReport.AverageQueueLengthBasisPoints >= 0);
    }

    [Fact]
    public void NextDay_UsesResetOperationalCounters()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 2);
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(0, 0),
            new BusinessSimulationOptions(
                baseArrivalBasisPoints: 10_000,
                basePurchaseBasisPoints: 10_000));

        simulation.AdvanceRealSeconds(1_440);
        var dayOne = Assert.Single(Assert.IsType<BusinessDayReport>(
            simulation.GetSnapshot().LastCompletedDay).Stores);
        simulation.AdvanceRealSeconds(1_440);
        var dayTwoReport = Assert.IsType<BusinessDayReport>(simulation.GetSnapshot().LastCompletedDay);
        var dayTwo = Assert.Single(dayTwoReport.Stores);

        Assert.Equal(1, dayOne.Visitors);
        Assert.Equal(1, dayOne.AcceptedPurchases);
        Assert.Equal(2, dayTwoReport.DayNumber);
        Assert.Equal(0, dayTwo.Visitors);
        Assert.Equal(0, dayTwo.AcceptedPurchases);
    }

    [Fact]
    public void ThirtyDayMultiStoreRun_IsDeterministicAndBounded()
    {
        var first = CreateStressSimulation(seed: 42);
        var second = CreateStressSimulation(seed: 42);

        first.AdvanceRealSeconds(30 * 1_440);
        second.AdvanceRealSeconds(30 * 1_440);
        var firstSnapshot = first.GetSnapshot();
        var secondSnapshot = second.GetSnapshot();

        Assert.Equal(43_200, firstSnapshot.GameMinute);
        Assert.Equal(30, Assert.IsType<BusinessDayReport>(firstSnapshot.LastCompletedDay).DayNumber);
        Assert.Equal(firstSnapshot.Business.CashCents, secondSnapshot.Business.CashCents);
        Assert.Equal(
            firstSnapshot.Stores.Select(StoreDeterministicValues),
            secondSnapshot.Stores.Select(StoreDeterministicValues));
        Assert.All(firstSnapshot.Stores, store =>
        {
            Assert.InRange(store.CleanlinessPermille, 0, 1_000);
            Assert.True(store.CheckoutQueueLength >= 0);
            Assert.True(store.CompletedSales >= 0);
        });
    }

    private static object StoreDeterministicValues(StoreOperationsSnapshot store) => new
    {
        store.StoreId,
        store.Visitors,
        store.AcceptedPurchases,
        store.CompletedSales,
        store.LostSales,
        store.CheckoutQueueLength,
        store.CleanlinessPermille,
        store.WagePaymentFailures
    };

    private static BusinessSimulation CreateStressSimulation(int seed)
    {
        var service = CreateService(openingCashCents: 1_000_000, includeSecondStore: true);
        service.OpenStore("station-store");
        service.PurchaseStock("corner-store", "water", 100);
        service.PurchaseStock("station-store", "water", 100);
        return new BusinessSimulation(
            service,
            [
                AssignCashier("corner-store", "cashier-a"),
                AssignCashier("station-store", "cashier-b")
            ],
            new DeterministicRandomSource(seed));
    }

    private static StoreEmployeeAssignment AssignCashier(string storeId, string employeeId) =>
        new(
            storeId,
            new Employee(
                new EmployeeId(employeeId),
                employeeId,
                EmployeeRole.Cashier,
                efficiencyPermille: 1_000,
                hourlyWage: new Money(60)));

    private static BusinessGameService CreateService(
        long openingCashCents = 100_000,
        bool includeSecondStore = false)
    {
        var stores = new List<ShopDefinition>
        {
            new(new ShopId("corner-store"), "街角店", 1, Money.Zero)
        };
        if (includeSecondStore)
        {
            stores.Add(new ShopDefinition(new ShopId("station-store"), "车站店", 1, Money.Zero));
        }

        return new BusinessGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 100, "ambient")],
            stores,
            new LevelCurve([0, 100]),
            starterShopId: "corner-store",
            openingCashCents,
            experiencePerItemSold: 1);
    }

    private sealed class DeterministicRandomSource(int seed)
        : HajimaoDesktopShop.Application.Simulation.IRandomSource
    {
        private uint _state = unchecked((uint)seed);

        public double NextDouble() => NextValue() / (double)uint.MaxValue;

        public int Next(int exclusiveMax) => checked((int)(NextValue() % (uint)exclusiveMax));

        private uint NextValue()
        {
            _state = unchecked(_state * 1_664_525u + 1_013_904_223u);
            return _state;
        }
    }
}
