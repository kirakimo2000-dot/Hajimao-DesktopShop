using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business;

public sealed class BusinessGameService
{
    private readonly object _gate = new();
    private readonly ProductDefinition[] _productDefinitions;
    private readonly Dictionary<string, ShopDefinition> _shopDefinitions;
    private readonly RetailBusiness _business;
    private readonly int _experiencePerItemSold;

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
    }

    public StockPurchaseResult PurchaseStock(string shopId, string productId, int quantity)
    {
        lock (_gate)
        {
            return GetShop(shopId).TryPurchaseStock(new ProductId(productId), quantity);
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
}
