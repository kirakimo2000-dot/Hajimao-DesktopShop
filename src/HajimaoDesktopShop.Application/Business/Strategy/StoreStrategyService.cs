using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Strategy;

public sealed class StoreStrategyService
{
    private readonly BusinessGameService _game;

    public StoreStrategyService(BusinessGameService game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public StoreStrategyCommandResult Apply(
        string storeId,
        StorePricingPreset pricing,
        StoreStockingPreset stocking)
    {
        var store = FindStore(storeId);
        if (store is null)
        {
            return new StoreStrategyCommandResult(
                StoreStrategyCommandStatus.UnknownStore,
                AppliedPlan: null);
        }

        if (store.Products.Count == 0)
        {
            return new StoreStrategyCommandResult(
                StoreStrategyCommandStatus.NoProducts,
                AppliedPlan: null);
        }

        var plan = StoreStrategyPlanner.Create(store, pricing, stocking);
        ValidateCompletePlan(store, plan);

        foreach (var product in plan.Products)
        {
            var priceResult = _game.ChangePrice(
                plan.StoreId,
                product.ProductId,
                product.SalePriceCents);
            if (priceResult.Status != PriceChangeStatus.Success)
            {
                throw new InvalidOperationException(
                    $"Validated strategy price failed for product '{product.ProductId}': {priceResult.Status}.");
            }

            _game.ConfigureAutoRestock(new AutoRestockPolicy(
                plan.StoreId,
                product.ProductId,
                IsEnabled: true,
                product.ReorderPoint,
                product.TargetQuantity,
                product.PreferredChannelId,
                product.UseEmergencySupplierWhenOutOfStock));
        }

        return new StoreStrategyCommandResult(StoreStrategyCommandStatus.Success, plan);
    }

    public StoreStrategyPlan? GetAppliedPlan(string storeId)
    {
        var store = FindStore(storeId);
        if (store is null || store.Products.Count == 0)
        {
            return null;
        }

        var policies = _game.GetProcurementSnapshot().AutoRestockPolicies
            .Where(policy => string.Equals(policy.StoreId, store.Id, StringComparison.Ordinal))
            .ToDictionary(policy => policy.ProductId, StringComparer.Ordinal);

        foreach (var pricing in Enum.GetValues<StorePricingPreset>())
        {
            foreach (var stocking in Enum.GetValues<StoreStockingPreset>())
            {
                var candidate = StoreStrategyPlanner.Create(store, pricing, stocking);
                if (Matches(store, policies, candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private BusinessStoreSnapshot? FindStore(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return null;
        }

        var normalized = storeId.Trim();
        return _game.GetSnapshot().Stores.SingleOrDefault(store =>
            string.Equals(store.Id, normalized, StringComparison.Ordinal));
    }

    private static void ValidateCompletePlan(
        BusinessStoreSnapshot store,
        StoreStrategyPlan plan)
    {
        if (plan.Products.Count != store.Products.Count
            || plan.Products.Any(product =>
                product.SalePriceCents <= 0
                || product.ReorderPoint < 0
                || product.TargetQuantity <= product.ReorderPoint
                || string.IsNullOrWhiteSpace(product.PreferredChannelId)))
        {
            throw new InvalidOperationException("The store strategy plan is incomplete or invalid.");
        }
    }

    private static bool Matches(
        BusinessStoreSnapshot store,
        IReadOnlyDictionary<string, AutoRestockPolicy> policies,
        StoreStrategyPlan candidate)
    {
        if (policies.Count != store.Products.Count)
        {
            return false;
        }

        foreach (var productPlan in candidate.Products)
        {
            var product = store.Products.Single(product => product.Id == productPlan.ProductId);
            if (product.SalePriceCents != productPlan.SalePriceCents
                || !policies.TryGetValue(productPlan.ProductId, out var policy)
                || !policy.IsEnabled
                || policy.ReorderPoint != productPlan.ReorderPoint
                || policy.TargetQuantity != productPlan.TargetQuantity
                || !string.Equals(
                    policy.PreferredChannelId,
                    productPlan.PreferredChannelId,
                    StringComparison.Ordinal)
                || policy.UseEmergencySupplierWhenOutOfStock
                    != productPlan.UseEmergencySupplierWhenOutOfStock)
            {
                return false;
            }
        }

        return true;
    }
}
