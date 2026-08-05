using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopBusinessSessionFactoryTests
{
    [Fact]
    public void CreateNew_StartsOneStoreWithCatalogProductsAndTwoStarterEmployees()
    {
        var products = CreateProducts(10);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero));
        var snapshot = start.Session.Simulation.GetSnapshot();

        Assert.True(start.IsNewGame);
        Assert.Null(start.OfflineSettlement);
        Assert.Equal(1, snapshot.Business.PlayerLevel);
        var store = Assert.Single(snapshot.Business.Stores);
        Assert.Equal("corner-store", store.Id);
        Assert.Equal(2, store.Products.Count);
        Assert.Equal(
            [EmployeeRole.Cashier, EmployeeRole.Restocker],
            snapshot.Employees.Employees.Select(employee => employee.Role));
        Assert.Equal(50_000, snapshot.Business.CashCents);
    }

    [Fact]
    public void Restore_UsesCompleteBusinessSaveAndDeterministicRandomState()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        original.Simulation.AdvanceRealSeconds(17);
        var save = original.CaptureSaveData(savedAtUtc);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc);

        Assert.False(start.IsNewGame);
        Assert.NotNull(start.OfflineSettlement);
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, start.Session.CaptureSaveData(save.SavedAtUtc), strict: true);
    }

    [Fact]
    public void Restore_AppliesElapsedTimeThroughOfflineSettlement()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        var save = original.CaptureSaveData(savedAtUtc);
        original.Simulation.AdvanceRealSeconds(10);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc.AddSeconds(10));

        var settlement = Assert.IsType<OfflineSettlementResult>(start.OfflineSettlement);
        Assert.Equal(10, settlement.RequestedSeconds);
        Assert.Equal(10, settlement.AppliedSeconds);
        Assert.False(settlement.WasCapped);
        Assert.Equal(OfflineTimeAnomaly.None, settlement.Anomaly);
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
    }

    [Fact]
    public void Restore_UsesConfiguredOfflineCap()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        var save = original.CaptureSaveData(savedAtUtc);
        original.Simulation.AdvanceRealSeconds(3);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc.AddMinutes(1),
            offlinePolicy: new OfflineSettlementPolicy(maxOfflineSeconds: 3, batchSize: 2));

        var settlement = Assert.IsType<OfflineSettlementResult>(start.OfflineSettlement);
        Assert.Equal(60, settlement.RequestedSeconds);
        Assert.Equal(3, settlement.AppliedSeconds);
        Assert.True(settlement.WasCapped);
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
    }

    [Fact]
    public void Restore_WhenClockMovedBackward_ReportsAnomalyWithoutAdvancing()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        var save = original.CaptureSaveData(savedAtUtc);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc.AddSeconds(-1));

        var settlement = Assert.IsType<OfflineSettlementResult>(start.OfflineSettlement);
        Assert.Equal(OfflineTimeAnomaly.ClockMovedBackward, settlement.Anomaly);
        Assert.Equal(0, settlement.AppliedSeconds);
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
    }

    private static ProductDefinition[] CreateProducts(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new ProductDefinition(
                $"product-{index}",
                $"商品 {index}",
                100 + index,
                200 + index,
                20,
                "ambient",
                requiredPlayerLevel: ((index - 1) / 2) + 1))
            .ToArray();
}
