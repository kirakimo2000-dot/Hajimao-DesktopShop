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

        var session = DesktopBusinessSessionFactory.Create(products, save: null, seed: 42);
        var snapshot = session.Simulation.GetSnapshot();

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
        var original = DesktopBusinessSessionFactory.Create(products, save: null, seed: 42);
        original.Simulation.AdvanceRealSeconds(17);
        var save = original.CaptureSaveData();

        var restored = DesktopBusinessSessionFactory.Create(products, save, seed: 999);

        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            restored.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, restored.CaptureSaveData(save.SavedAtUtc), strict: true);
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
