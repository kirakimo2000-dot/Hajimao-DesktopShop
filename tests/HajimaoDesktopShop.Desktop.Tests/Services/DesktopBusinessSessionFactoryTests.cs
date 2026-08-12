using HajimaoDesktopShop.Application.Catalog;
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
        Assert.Equal(1, snapshot.Business.PlayerLevel);
        var store = Assert.Single(snapshot.Business.Stores);
        Assert.Equal("corner-store", store.Id);
        Assert.Equal(2, store.Products.Count);
        Assert.Equal(
            [EmployeeRole.Cashier, EmployeeRole.Restocker],
            snapshot.Employees.Employees.Select(employee => employee.Role));
        Assert.Equal(DesktopGameContent.OpeningCashCents, snapshot.Business.CashCents);
        Assert.All(snapshot.Employees.Employees, employee =>
        {
            Assert.False(employee.IsAlwaysOn);
            Assert.Equal(DesktopGameContent.StarterShiftStartMinute, employee.ShiftStartMinute);
            Assert.Equal(DesktopGameContent.StarterShiftEndMinute, employee.ShiftEndMinute);
        });
    }

    [Fact]
    public void CreateNew_WithStoreContent_AppliesFamousBrandAndFormatEconomics()
    {
        var content = CreateStoreContent();

        var start = DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero),
            storeContent: content);
        var store = Assert.Single(start.Session.Game.GetSnapshot().Stores);

        Assert.Equal("seven-eleven", store.StoreBrandId);
        Assert.Equal("convenience", store.StoreFormatId);
        Assert.Equal(1_100, store.FormatEconomics!.DemandSensitivity.BaseDemandPermille);
        Assert.Equal(22, store.Products.First().Capacity);
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
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, start.Session.CaptureSaveData(save.SavedAtUtc), strict: true);
    }

    [Fact]
    public void Restore_DoesNotAdvanceWhileTheApplicationWasClosed()
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
            nowUtc: savedAtUtc.AddSeconds(10));

        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, start.Session.CaptureSaveData(save.SavedAtUtc), strict: true);
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

    private static StoreContentCatalog CreateStoreContent() =>
        new(
            [
                new StoreFormatDefinition(
                    "convenience",
                    "社区便利",
                    40_000,
                    40_000,
                    1_100,
                    1_000,
                    1_000,
                    1_000,
                    1_000,
                    1_100,
                    "steady",
                    new Dictionary<string, int>
                    {
                        ["ambient"] = 1_000,
                        ["chilled"] = 1_000,
                        ["frozen"] = 1_000
                    },
                    Application.Business.Strategy.StorePricingPreset.Balanced,
                    Application.Business.Strategy.StoreStockingPreset.Balanced)
            ],
            [
                new StoreBrandDefinition(
                    "seven-eleven",
                    "7-Eleven",
                    "global",
                    "convenience",
                    "facade-convenience-a",
                    "real-world-name",
                    "review-required")
            ]);
}
