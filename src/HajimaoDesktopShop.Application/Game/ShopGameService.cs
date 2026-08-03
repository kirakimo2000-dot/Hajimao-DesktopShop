using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Game;

public sealed class ShopGameService
{
    private readonly object _gate = new();
    private readonly Shop _shop;
    private readonly List<ProductId> _productOrder = [];
    private readonly Dictionary<ProductId, string> _shelfKinds = [];

    public ShopGameService(IEnumerable<ProductDefinition> definitions, long openingCashCents)
        : this(definitions, new Shop(new Money(openingCashCents)), restoredState: null)
    {
    }

    public ShopGameService(IEnumerable<ProductDefinition> definitions, ShopSaveData restoredState)
        : this(
            definitions,
            Shop.Restore(new ShopFinancialState(
                new Money(restoredState?.CashCents ?? throw new ArgumentNullException(nameof(restoredState))),
                new Money(restoredState.TotalRevenueCents),
                new Money(restoredState.TotalStockPurchaseCostCents),
                new Money(restoredState.TotalGrossProfitCents))),
            restoredState)
    {
    }

    private ShopGameService(
        IEnumerable<ProductDefinition> definitions,
        Shop shop,
        ShopSaveData? restoredState)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        _shop = shop;
        var restoredProducts = restoredState?.Products.ToDictionary(product => product.ProductId, StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var id = new ProductId(definition.Id);
            ProductSaveData? savedProduct = null;
            restoredProducts?.TryGetValue(definition.Id, out savedProduct);
            var product = new Product(
                id,
                definition.Name,
                new Money(definition.WholesalePriceCents),
                new Money(savedProduct?.SalePriceCents ?? definition.InitialSalePriceCents));

            _shop.RegisterProduct(product, definition.Capacity, savedProduct?.Quantity ?? 0);
            _productOrder.Add(id);
            _shelfKinds.Add(id, definition.ShelfKind);
        }

        if (_productOrder.Count == 0)
        {
            throw new ArgumentException("At least one product definition is required.", nameof(definitions));
        }
    }

    public StockPurchaseResult PurchaseStock(string productId, int quantity)
    {
        lock (_gate)
        {
            return _shop.TryPurchaseStock(new ProductId(productId), quantity);
        }
    }

    public PriceChangeResult ChangePrice(string productId, long salePriceCents)
    {
        lock (_gate)
        {
            return _shop.TryChangePrice(new ProductId(productId), new Money(salePriceCents));
        }
    }

    public SaleResult Sell(string productId, int quantity)
    {
        lock (_gate)
        {
            return _shop.TrySell(new ProductId(productId), quantity);
        }
    }

    public ShopSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var products = _productOrder
                .Select(CreateProductSnapshot)
                .ToArray();

            return new ShopSnapshot(
                _shop.Cash.Cents,
                _shop.TotalRevenue.Cents,
                _shop.TotalStockPurchaseCost.Cents,
                _shop.TotalGrossProfit.Cents,
                Array.AsReadOnly(products));
        }
    }

    private ProductSnapshot CreateProductSnapshot(ProductId productId)
    {
        var slot = _shop.GetInventory(productId);
        return new ProductSnapshot(
            productId.Value,
            slot.Product.Name,
            slot.Product.WholesalePrice.Cents,
            slot.Product.SalePrice.Cents,
            slot.Quantity,
            slot.Capacity,
            _shelfKinds[productId]);
    }
}
