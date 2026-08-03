using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business;

public sealed class BusinessGameService : IProcurementStockGateway
{
    private readonly object _gate = new();
    private readonly ProductDefinition[] _productDefinitions;
    private readonly Dictionary<string, ShopDefinition> _shopDefinitions;
    private readonly RetailBusiness _business;
    private readonly int _experiencePerItemSold;
    private readonly BusinessProcurementService _procurement;

    public BusinessGameService(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        string starterShopId,
        long openingCashCents,
        int experiencePerItemSold = 1)
    {
        ArgumentNullException.ThrowIfNull(productDefinitions);
        ArgumentNullException.ThrowIfNull(shopDefinitions);
        ArgumentNullException.ThrowIfNull(levelCurve);
        if (string.IsNullOrWhiteSpace(starterShopId))
        {
            throw new ArgumentException("Starter shop ID is required.", nameof(starterShopId));
        }

        if (openingCashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingCashCents));
        }

        if (experiencePerItemSold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experiencePerItemSold));
        }

        _productDefinitions = productDefinitions.ToArray();
        if (_productDefinitions.Length == 0
            || _productDefinitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count()
                != _productDefinitions.Length)
        {
            throw new ArgumentException(
                "Product definitions must contain unique products.",
                nameof(productDefinitions));
        }

        var stores = shopDefinitions.ToArray();
        _shopDefinitions = stores.ToDictionary(
            definition => definition.Id.Value,
            StringComparer.Ordinal);
        if (_shopDefinitions.Count == 0 || _shopDefinitions.Count != stores.Length)
        {
            throw new ArgumentException(
                "Shop definitions must contain unique shops.",
                nameof(shopDefinitions));
        }

        if (!_shopDefinitions.TryGetValue(starterShopId.Trim(), out var starterDefinition))
        {
            throw new ArgumentException("Starter shop definition was not found.", nameof(starterShopId));
        }

        _experiencePerItemSold = experiencePerItemSold;
        _business = RetailBusiness.Start(
            new PlayerProfile(levelCurve),
            new Money(openingCashCents),
            starterDefinition);
        RegisterUnlockedProducts(_business.GetShop(starterDefinition.Id));
        _procurement = new BusinessProcurementService(this);
    }

    public BusinessGameService(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        BusinessSaveData restoredState,
        int experiencePerItemSold = 1)
    {
        ArgumentNullException.ThrowIfNull(productDefinitions);
        ArgumentNullException.ThrowIfNull(shopDefinitions);
        ArgumentNullException.ThrowIfNull(levelCurve);
        ArgumentNullException.ThrowIfNull(restoredState);
        if (restoredState.TotalExperience < 0 || restoredState.CashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restoredState));
        }

        if (experiencePerItemSold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experiencePerItemSold));
        }

        _productDefinitions = productDefinitions.ToArray();
        if (_productDefinitions.Length == 0
            || _productDefinitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count()
                != _productDefinitions.Length)
        {
            throw new ArgumentException(
                "Product definitions must contain unique products.",
                nameof(productDefinitions));
        }

        var stores = shopDefinitions.ToArray();
        _shopDefinitions = stores.ToDictionary(
            definition => definition.Id.Value,
            StringComparer.Ordinal);
        if (_shopDefinitions.Count == 0 || _shopDefinitions.Count != stores.Length)
        {
            throw new ArgumentException(
                "Shop definitions must contain unique shops.",
                nameof(shopDefinitions));
        }

        var savedStores = restoredState.Stores?.ToArray()
            ?? throw new ArgumentException("Restored stores are required.", nameof(restoredState));
        if (savedStores.Length == 0 || savedStores.Any(store => store is null))
        {
            throw new ArgumentException("At least one restored store is required.", nameof(restoredState));
        }

        var duplicateStore = savedStores
            .GroupBy(store => store.StoreId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateStore is not null)
        {
            throw new ArgumentException(
                $"Restored store '{duplicateStore.Key}' is duplicated.",
                nameof(restoredState));
        }

        var player = new PlayerProfile(levelCurve, restoredState.TotalExperience);
        var restoredStores = savedStores.Select(store =>
        {
            if (string.IsNullOrWhiteSpace(store.StoreId)
                || !_shopDefinitions.TryGetValue(store.StoreId, out var definition))
            {
                throw new ArgumentException(
                    $"Restored store '{store.StoreId}' has no definition.",
                    nameof(restoredState));
            }

            return new RetailBusinessStoreState(
                definition,
                new ShopFinancialState(
                    new Money(restoredState.CashCents),
                    new Money(store.RevenueCents),
                    new Money(store.StockPurchaseCostCents),
                    new Money(store.GrossProfitCents),
                    new Money(store.WageCostCents)));
        }).ToArray();

        _experiencePerItemSold = experiencePerItemSold;
        _business = RetailBusiness.Restore(
            player,
            new Money(restoredState.CashCents),
            restoredStores);

        var definitionsById = _productDefinitions.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal);
        foreach (var savedStore in savedStores)
        {
            var shop = _business.GetShop(new ShopId(savedStore.StoreId));
            var savedProducts = savedStore.Products?.ToArray()
                ?? throw new ArgumentException("Restored products are required.", nameof(restoredState));
            var duplicateProduct = savedProducts
                .GroupBy(product => product.ProductId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateProduct is not null)
            {
                throw new ArgumentException(
                    $"Restored product '{duplicateProduct.Key}' is duplicated in store '{savedStore.StoreId}'.",
                    nameof(restoredState));
            }

            foreach (var savedProduct in savedProducts)
            {
                if (savedProduct is null
                    || !definitionsById.TryGetValue(savedProduct.ProductId, out var definition))
                {
                    throw new ArgumentException(
                        $"Restored product '{savedProduct?.ProductId}' has no definition.",
                        nameof(restoredState));
                }

                shop.RegisterProduct(
                    new Product(
                        new ProductId(definition.Id),
                        definition.Name,
                        new Money(definition.WholesalePriceCents),
                        new Money(savedProduct.SalePriceCents)),
                    definition.Capacity,
                    savedProduct.Quantity);
            }

            RegisterUnlockedProducts(shop);
        }

        _procurement = new BusinessProcurementService(this);
    }

    public StockPurchaseResult PurchaseStock(string shopId, string productId, int quantity)
    {
        lock (_gate)
        {
            return GetShop(shopId).TryPurchaseStock(new ProductId(productId), quantity);
        }
    }

    public ProcurementOrderResult PlaceProcurementOrder(
        string shopId,
        string productId,
        string channelId,
        int quantity)
    {
        lock (_gate)
        {
            return _procurement.PlaceOrder(shopId, productId, channelId, quantity, isAutomatic: false);
        }
    }

    public void AdvanceProcurementMinute()
    {
        lock (_gate)
        {
            _procurement.AdvanceMinute();
        }
    }

    public void ConfigureAutoRestock(AutoRestockPolicy policy)
    {
        lock (_gate)
        {
            _procurement.ConfigureAutoRestock(policy);
        }
    }

    public ProcurementSnapshot GetProcurementSnapshot()
    {
        lock (_gate)
        {
            return _procurement.GetSnapshot();
        }
    }

    public Money QuoteProcurementUnitCost(string productId, string channelId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        lock (_gate)
        {
            var definition = _productDefinitions.SingleOrDefault(item =>
                string.Equals(item.Id, productId.Trim(), StringComparison.Ordinal))
                ?? throw new KeyNotFoundException($"Product '{productId.Trim()}' was not found.");
            return _procurement.QuoteUnitCost(new Money(definition.WholesalePriceCents), channelId);
        }
    }

    public BusinessSaleResult Sell(string shopId, string productId, int quantity)
    {
        lock (_gate)
        {
            var previousLevel = _business.Player.Level;
            var sale = GetShop(shopId).TrySell(new ProductId(productId), quantity);
            if (sale.Status != SaleStatus.Success)
            {
                return new BusinessSaleResult(sale, previousLevel, previousLevel, []);
            }

            _business.Player.GainExperience(checked((long)quantity * _experiencePerItemSold));
            var unlocked = RegisterUnlockedProductsForAllStores();
            return new BusinessSaleResult(
                sale,
                previousLevel,
                _business.Player.Level,
                Array.AsReadOnly(unlocked.ToArray()));
        }
    }

    public PriceChangeResult ChangePrice(string shopId, string productId, long salePriceCents)
    {
        lock (_gate)
        {
            return GetShop(shopId).TryChangePrice(new ProductId(productId), new Money(salePriceCents));
        }
    }

    public WagePaymentResult PayEmployeeMinute(string shopId, Employee employee)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            throw new ArgumentException("Shop ID is required.", nameof(shopId));
        }

        ArgumentNullException.ThrowIfNull(employee);
        lock (_gate)
        {
            return _business.TryPayEmployeeMinute(new ShopId(shopId), employee);
        }
    }

    public OpenShopResult OpenStore(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            throw new ArgumentException("Shop ID is required.", nameof(shopId));
        }

        lock (_gate)
        {
            var id = new ShopId(shopId);
            if (!_shopDefinitions.TryGetValue(id.Value, out var definition))
            {
                return new OpenShopResult(OpenShopStatus.UnknownDefinition, id, Money.Zero);
            }

            var result = _business.TryOpenStore(definition);
            if (result.Status == OpenShopStatus.Success)
            {
                RegisterUnlockedProducts(_business.GetShop(id));
            }

            return result;
        }
    }

    public BusinessSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var stores = _business.StoreIds
                .Select(CreateStoreSnapshot)
                .ToArray();
            return new BusinessSnapshot(
                _business.Player.Level,
                _business.Player.TotalExperience,
                _business.Cash.Cents,
                Array.AsReadOnly(stores));
        }
    }

    public bool ContainsStoreDefinition(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            return false;
        }

        lock (_gate)
        {
            return _shopDefinitions.ContainsKey(shopId.Trim());
        }
    }

    public BusinessSaveData CaptureSaveData()
    {
        lock (_gate)
        {
            var snapshot = GetSnapshot();
            var stores = snapshot.Stores
                .OrderBy(store => store.Id, StringComparer.Ordinal)
                .Select(store => new BusinessStoreSaveData(
                    store.Id,
                    store.RevenueCents,
                    store.StockPurchaseCostCents,
                    store.GrossProfitCents,
                    store.WageCostCents,
                    Array.AsReadOnly(store.Products
                        .OrderBy(product => product.Id, StringComparer.Ordinal)
                        .Select(product => new BusinessProductSaveData(
                            product.Id,
                            product.SalePriceCents,
                            product.Quantity))
                        .ToArray())))
                .ToArray();
            return new BusinessSaveData(
                snapshot.TotalExperience,
                snapshot.CashCents,
                Array.AsReadOnly(stores));
        }
    }

    private Shop GetShop(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            throw new ArgumentException("Shop ID is required.", nameof(shopId));
        }

        return _business.GetShop(new ShopId(shopId));
    }

    private List<string> RegisterUnlockedProductsForAllStores()
    {
        var unlocked = new List<string>();
        foreach (var shopId in _business.StoreIds)
        {
            RegisterUnlockedProducts(_business.GetShop(shopId), unlocked);
        }

        return unlocked.Distinct(StringComparer.Ordinal).ToList();
    }

    private void RegisterUnlockedProducts(Shop shop, List<string>? unlocked = null)
    {
        foreach (var definition in _productDefinitions)
        {
            var productId = new ProductId(definition.Id);
            if (definition.RequiredPlayerLevel > _business.Player.Level || shop.ContainsProduct(productId))
            {
                continue;
            }

            shop.RegisterProduct(
                new Product(
                    productId,
                    definition.Name,
                    new Money(definition.WholesalePriceCents),
                    new Money(definition.InitialSalePriceCents)),
                definition.Capacity);
            unlocked?.Add(definition.Id);
        }
    }

    private BusinessStoreSnapshot CreateStoreSnapshot(ShopId shopId)
    {
        var shop = _business.GetShop(shopId);
        var products = _productDefinitions
            .Where(definition => shop.ContainsProduct(new ProductId(definition.Id)))
            .Select(definition => CreateProductSnapshot(shop, definition))
            .ToArray();
        return new BusinessStoreSnapshot(
            shopId.Value,
            _shopDefinitions[shopId.Value].Name,
            shop.TotalRevenue.Cents,
            shop.TotalStockPurchaseCost.Cents,
            shop.TotalGrossProfit.Cents,
            Array.AsReadOnly(products),
            shop.TotalWageCost.Cents,
            shop.TotalNetProfit.Cents);
    }

    private static ProductSnapshot CreateProductSnapshot(Shop shop, ProductDefinition definition)
    {
        var slot = shop.GetInventory(new ProductId(definition.Id));
        return new ProductSnapshot(
            definition.Id,
            definition.Name,
            slot.Product.WholesalePrice.Cents,
            slot.Product.SalePrice.Cents,
            slot.Quantity,
            slot.Capacity,
            definition.ShelfKind,
            definition.RequiredPlayerLevel,
            slot.Product.UnitGrossProfit.Cents,
            slot.Product.GrossMarginBasisPoints,
            definition.InitialSalePriceCents);
    }

    bool IProcurementStockGateway.ContainsOpenStore(string storeId) =>
        _business.StoreIds.Contains(new ShopId(storeId));

    ProcurementProductState? IProcurementStockGateway.FindProduct(
        string storeId,
        string productId)
    {
        var shopId = new ShopId(storeId);
        if (!_business.StoreIds.Contains(shopId))
        {
            return null;
        }

        var shop = _business.GetShop(shopId);
        var id = new ProductId(productId);
        if (!shop.ContainsProduct(id))
        {
            return null;
        }

        var slot = shop.GetInventory(id);
        return new ProcurementProductState(
            storeId,
            productId,
            slot.Quantity,
            slot.Capacity,
            slot.Product.WholesalePrice);
    }

    StockPurchaseResult IProcurementStockGateway.TryPayForStockOrder(
        string storeId,
        string productId,
        int quantity,
        Money unitCost) =>
        GetShop(storeId).TryPayForStockOrder(new ProductId(productId), quantity, unitCost);

    StockReceiptResult IProcurementStockGateway.TryReceivePaidStock(
        string storeId,
        string productId,
        int quantity) =>
        GetShop(storeId).TryReceivePaidStock(new ProductId(productId), quantity);
}
