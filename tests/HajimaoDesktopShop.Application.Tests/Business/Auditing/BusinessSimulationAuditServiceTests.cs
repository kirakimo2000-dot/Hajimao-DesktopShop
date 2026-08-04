using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Auditing;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Auditing;

public sealed class BusinessSimulationAuditServiceTests
{
    [Fact]
    public void Run_RejectsInvalidDurationsAndBatchSizes()
    {
        var simulation = CreateSimulation().Simulation;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BusinessSimulationAuditService.Run(simulation, seconds: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationAuditOptions(batchSize: 0));
    }

    [Fact]
    public void Run_ZeroSecondsReturnsAnUnchangedBaselineReport()
    {
        var simulation = CreateSimulation(openSecondStore: true).Simulation;
        var before = simulation.GetSnapshot();

        var report = BusinessSimulationAuditService.Run(simulation, seconds: 0);

        Assert.Equal(0, report.RequestedSeconds);
        Assert.Equal(0, report.AppliedSeconds);
        Assert.Equal(0, report.BatchCount);
        Assert.Equal(report.StartingGameMinute, report.EndingGameMinute);
        Assert.Equal(0, report.CashDeltaCents);
        Assert.Equal(0, report.TotalExperienceDelta);
        Assert.All(report.Stores, store =>
        {
            Assert.Equal(0, store.VisitorsDelta);
            Assert.Equal(0, store.CompletedSalesDelta);
            Assert.Equal(0, store.RevenueDeltaCents);
        });
        Assert.Equivalent(before, simulation.GetSnapshot(), strict: true);
    }

    [Fact]
    public void Run_ReportsStableMultiStoreDeltasFromBeforeAndAfterSnapshots()
    {
        var simulation = CreateSimulation(openSecondStore: true).Simulation;
        var before = simulation.GetSnapshot();

        var report = BusinessSimulationAuditService.Run(
            simulation,
            seconds: 25,
            new BusinessSimulationAuditOptions(batchSize: 10));
        var after = simulation.GetSnapshot();

        Assert.Equal(25, report.RequestedSeconds);
        Assert.Equal(25, report.AppliedSeconds);
        Assert.Equal(3, report.BatchCount);
        Assert.Equal(before.GameMinute, report.StartingGameMinute);
        Assert.Equal(after.GameMinute, report.EndingGameMinute);
        Assert.Equal(25, report.EndingGameMinute - report.StartingGameMinute);
        Assert.Equal(after.Business.CashCents - before.Business.CashCents, report.CashDeltaCents);
        Assert.Equal(
            after.Business.TotalExperience - before.Business.TotalExperience,
            report.TotalExperienceDelta);
        Assert.Equal(["alpha-store", "zeta-store"], report.Stores.Select(store => store.StoreId));

        foreach (var storeReport in report.Stores)
        {
            var beforeRuntime = before.Stores.Single(store => store.StoreId == storeReport.StoreId);
            var afterRuntime = after.Stores.Single(store => store.StoreId == storeReport.StoreId);
            var beforeBusiness = before.Business.Stores.Single(store => store.Id == storeReport.StoreId);
            var afterBusiness = after.Business.Stores.Single(store => store.Id == storeReport.StoreId);
            Assert.Equal(afterRuntime.Visitors - beforeRuntime.Visitors, storeReport.VisitorsDelta);
            Assert.Equal(
                afterRuntime.AcceptedPurchases - beforeRuntime.AcceptedPurchases,
                storeReport.AcceptedPurchasesDelta);
            Assert.Equal(
                afterRuntime.CompletedSales - beforeRuntime.CompletedSales,
                storeReport.CompletedSalesDelta);
            Assert.Equal(afterRuntime.LostSales - beforeRuntime.LostSales, storeReport.LostSalesDelta);
            Assert.Equal(
                afterRuntime.WagePaymentFailures - beforeRuntime.WagePaymentFailures,
                storeReport.WagePaymentFailuresDelta);
            Assert.Equal(
                afterBusiness.RevenueCents - beforeBusiness.RevenueCents,
                storeReport.RevenueDeltaCents);
            Assert.Equal(
                afterBusiness.GrossProfitCents - beforeBusiness.GrossProfitCents,
                storeReport.GrossProfitDeltaCents);
            Assert.Equal(
                afterBusiness.WageCostCents - beforeBusiness.WageCostCents,
                storeReport.WageCostDeltaCents);
            Assert.Equal(
                afterBusiness.OperatingCostCents - beforeBusiness.OperatingCostCents,
                storeReport.OperatingCostDeltaCents);
            Assert.Equal(
                afterBusiness.NetProfitCents - beforeBusiness.NetProfitCents,
                storeReport.NetProfitDeltaCents);
            Assert.Equal(afterRuntime.CheckoutQueueLength, storeReport.EndingCheckoutQueueLength);
            Assert.Equal(afterRuntime.CleanlinessPermille, storeReport.EndingCleanlinessPermille);
            Assert.Equal(
                afterBusiness.Products.Sum(product => product.Quantity),
                storeReport.EndingInventoryUnits);
        }
    }

    [Fact]
    public void Run_FromIdenticalSavedStateProducesIdenticalReportAndFinalState()
    {
        var initial = CreateSimulation(openSecondStore: true);
        initial.Simulation.AdvanceRealSeconds(7);
        var businessSave = initial.Service.CaptureSaveData();
        var simulationSave = initial.Simulation.CaptureSaveData();
        var first = RestoreSimulation(businessSave, simulationSave);
        var second = RestoreSimulation(businessSave, simulationSave);

        var firstReport = BusinessSimulationAuditService.Run(
            first.Simulation,
            seconds: 120,
            new BusinessSimulationAuditOptions(batchSize: 17));
        var secondReport = BusinessSimulationAuditService.Run(
            second.Simulation,
            seconds: 120,
            new BusinessSimulationAuditOptions(batchSize: 17));

        Assert.Equivalent(firstReport, secondReport, strict: true);
        Assert.Equivalent(
            first.Service.CaptureSaveData(),
            second.Service.CaptureSaveData(),
            strict: true);
        Assert.Equivalent(
            first.Simulation.CaptureSaveData(),
            second.Simulation.CaptureSaveData(),
            strict: true);
    }

    private static (BusinessGameService Service, BusinessSimulation Simulation) CreateSimulation(
        bool openSecondStore = false)
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 500);
        var assignments = new List<StoreEmployeeAssignment>
        {
            AssignCashier("zeta-store", "cashier-z")
        };
        if (openSecondStore)
        {
            Assert.Equal(OpenShopStatus.Success, service.OpenStore("alpha-store").Status);
            service.PurchaseStock("alpha-store", "water", 500);
            assignments.Add(AssignCashier("alpha-store", "cashier-a"));
        }

        return (
            service,
            new BusinessSimulation(
                service,
                assignments,
                new StatefulTestRandomSource(77),
                CreateOptions()));
    }

    private static (BusinessGameService Service, BusinessSimulation Simulation) RestoreSimulation(
        BusinessSaveData businessSave,
        BusinessSimulationSaveData simulationSave)
    {
        var service = CreateService(businessSave);
        return (
            service,
            new BusinessSimulation(
                service,
                simulationSave,
                new StatefulTestRandomSource(1),
                CreateOptions()));
    }

    private static BusinessGameService CreateService(BusinessSaveData? restored = null)
    {
        ProductDefinition[] products =
        [
            new("water", "矿泉水", 100, 200, 1_000, "ambient")
        ];
        ShopDefinition[] stores =
        [
            new(new ShopId("zeta-store"), "街角店", 1, Money.Zero),
            new(new ShopId("alpha-store"), "车站店", 1, Money.Zero)
        ];
        var curve = new LevelCurve([0]);
        return restored is null
            ? new BusinessGameService(products, stores, curve, "zeta-store", 20_000_000)
            : new BusinessGameService(products, stores, curve, restored);
    }

    private static StoreEmployeeAssignment AssignCashier(string storeId, string employeeId) =>
        new(
            storeId,
            new Employee(
                new EmployeeId(employeeId),
                employeeId,
                EmployeeRole.Cashier,
                1_000,
                new Money(60)));

    private static BusinessSimulationOptions CreateOptions() =>
        new(baseArrivalBasisPoints: 8_000, basePurchaseBasisPoints: 10_000);
}
