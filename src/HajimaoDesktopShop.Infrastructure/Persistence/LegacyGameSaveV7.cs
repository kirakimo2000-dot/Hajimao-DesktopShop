using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal static class LegacyGameSaveV7
{
    private const string FallbackStoreId = "corner-store";

    public static GameSaveData UpgradeToV8(GameSaveData legacy)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        if (legacy.SchemaVersion != 7)
        {
            throw new InvalidDataException($"Expected schema 7, found {legacy.SchemaVersion}.");
        }

        var sourceStores = legacy.Business?.Stores is { Count: > 0 } businessStores
            ? businessStores
            :
            [
                new BusinessStoreSaveData(
                    FallbackStoreId,
                    legacy.Shop.TotalRevenueCents,
                    legacy.Shop.TotalStockPurchaseCostCents,
                    legacy.Shop.TotalGrossProfitCents,
                    0,
                    legacy.Shop.Products
                        .Select(product => new BusinessProductSaveData(
                            product.ProductId,
                            product.SalePriceCents,
                            product.Quantity))
                        .ToArray())
            ];

        var refundCents = CalculateProcurementRefund(legacy.Business?.Procurement);
        var upgradedBusiness = UpgradeBusiness(legacy.Business, refundCents);
        var upgradedShop = legacy.Shop with
        {
            CashCents = checked(legacy.Shop.CashCents + refundCents),
            Products = legacy.Shop.Products
                .Select(product => product with { Quantity = 0 })
                .ToArray()
        };
        var upgradedSimulation = legacy.Simulation with
        {
            RestockQueue = [],
            ActiveRestockTask = null,
            LastRestockFailure = null
        };
        var archivedEmployees = (legacy.BusinessSimulation?.Employees ?? [])
            .OrderBy(employee => employee.StoreId, StringComparer.Ordinal)
            .ThenBy(employee => employee.EmployeeId, StringComparer.Ordinal)
            .Select(employee => new LegacyEmployeeArchiveEntry(
                employee.StoreId,
                employee.EmployeeId,
                employee.Name,
                "maomao-default"))
            .ToArray();
        var upgradedBusinessSimulation = legacy.BusinessSimulation is null
            ? null
            : legacy.BusinessSimulation with { Employees = [] };

        return legacy with
        {
            SchemaVersion = 8,
            Shop = upgradedShop,
            Simulation = upgradedSimulation,
            Business = upgradedBusiness,
            BusinessSimulation = upgradedBusinessSimulation,
            Combat = CreateCombat(sourceStores, legacy.BusinessSimulation?.RandomState ?? 0, archivedEmployees)
        };
    }

    private static long CalculateProcurementRefund(BusinessProcurementSaveData? procurement) =>
        procurement?.PendingOrders
            .Where(order => order.Status is ProcurementOrderStatus.InTransit or ProcurementOrderStatus.AwaitingSpace)
            .Aggregate(0L, (sum, order) => checked(sum + checked(order.UnitCostCents * order.Quantity)))
        ?? 0;

    private static BusinessSaveData? UpgradeBusiness(BusinessSaveData? business, long refundCents)
    {
        if (business is null)
        {
            return null;
        }

        var procurement = business.Procurement is null
            ? null
            : business.Procurement with
            {
                PendingOrders = [],
                AutoRestockPolicies = []
            };
        var stores = business.Stores
            .Select(store => store with
            {
                Products = store.Products
                    .Select(product => product with { Quantity = 0 })
                    .ToArray(),
                Development = store.Development is null
                    ? null
                    : store.Development with { ShelfLevel = 0 }
            })
            .ToArray();
        return business with
        {
            CashCents = checked(business.CashCents + refundCents),
            Stores = stores,
            Procurement = procurement
        };
    }

    private static CombatSaveData CreateCombat(
        IReadOnlyList<BusinessStoreSaveData> stores,
        ulong randomState,
        IReadOnlyList<LegacyEmployeeArchiveEntry> archivedEmployees)
    {
        var collection = new ProductCollection();
        foreach (var productGroup in stores
            .SelectMany(store => store.Products)
            .GroupBy(product => product.ProductId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            collection.RegisterCopy(productGroup.Key);
            var copies = productGroup.Sum(product => Math.Max(0, product.Quantity));
            for (var index = 0; index < copies; index++)
            {
                collection.RegisterCopy(productGroup.Key);
            }
        }

        var loadouts = stores
            .OrderBy(store => store.StoreId, StringComparer.Ordinal)
            .Select(store => new StoreProductLoadoutSaveData(
                store.StoreId,
                3,
                store.Products
                    .Select(product => product.ProductId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(productId => productId, StringComparer.Ordinal)
                    .Take(3)
                    .ToArray()))
            .ToArray();
        var combatStores = stores
            .OrderBy(store => store.StoreId, StringComparer.Ordinal)
            .Select(store => new StoreCombatStateSaveData(store.StoreId, StoreCombatState.Empty))
            .ToArray();
        return new CombatSaveData(
            new ProductCollectionSaveData(collection.Entries),
            loadouts,
            combatStores,
            randomState,
            new LegacyCombatCompatibilitySaveData(archivedEmployees));
    }
}
