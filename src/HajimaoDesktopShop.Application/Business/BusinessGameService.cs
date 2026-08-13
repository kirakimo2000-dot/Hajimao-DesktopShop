using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business;

public sealed class BusinessGameService :
    IProcurementStockGateway,
    IEmployeeOperationsGateway,
    IStoreGrowthGateway
{
    private const int MaximumBaseProductAssortment = 12;
    private readonly object _gate = new();
    private readonly ProductDefinition[] _productDefinitions;
    private readonly Dictionary<string, ShopDefinition> _shopDefinitions;
    private readonly Dictionary<string, ProductDefinition[]> _assortmentsByStore = new(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, StoreFormatDefinition> _storeFormats;
    private readonly IReadOnlyDictionary<string, StoreBrandDefinition> _storeBrands;
    private readonly RetailBusiness _business;
    private readonly int _experiencePerItemSold;
    private readonly BusinessProcurementService _procurement;
    private readonly StoreGrowthService _storeGrowth;

    public BusinessGameService(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        string starterShopId,
        long openingCashCents,
        int experiencePerItemSold = 1,
        StoreContentCatalog? storeContent = null)
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

        _storeFormats = CreateStoreFormatIndex(storeContent);
        _storeBrands = CreateStoreBrandIndex(storeContent);

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
        _storeGrowth = new StoreGrowthService(this);
        ConfigureDefaultAutomaticStocking(starterDefinition.Id.Value);
    }

    public BusinessGameService(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        BusinessSaveData restoredState,
        int experiencePerItemSold = 1,
        StoreContentCatalog? storeContent = null)
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

        _storeFormats = CreateStoreFormatIndex(storeContent);
        _storeBrands = CreateStoreBrandIndex(storeContent);

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

        foreach (var savedStore in savedStores)
        {
            if (savedStore.StreetOrdinal <= 0
                || string.IsNullOrWhiteSpace(savedStore.StoreBrandId)
                || string.IsNullOrWhiteSpace(savedStore.StoreFormatId))
            {
                continue;
            }

            var fallbackName = _shopDefinitions.TryGetValue(savedStore.StoreId, out var existing)
                ? existing.Name
                : savedStore.StoreId;
            _shopDefinitions[savedStore.StoreId] = new ShopDefinition(
                new ShopId(savedStore.StoreId),
                new StoreBrandId(savedStore.StoreBrandId),
                new StoreFormatId(savedStore.StoreFormatId),
                string.IsNullOrWhiteSpace(savedStore.StoreName)
                    ? fallbackName
                    : savedStore.StoreName,
                savedStore.StreetOrdinal,
                Money.Zero);
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
                    new Money(store.WageCostCents),
                    new Money(store.OperatingCostCents)),
                store.Development is null
                    ? null
                    : new StoreDevelopmentState(
                        store.Development.ExpansionLevel,
                        store.Development.ShelfLevel,
                        store.Development.DecorationLevel));
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
            var format = CreateFormatEconomics(_shopDefinitions[savedStore.StoreId].FormatId.Value);
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
                    ScaleCapacity(definition.Capacity, format.InventoryCapacityPermille),
                    savedProduct.Quantity);
            }

            RegisterUnlockedProducts(shop);
        }

        _procurement = new BusinessProcurementService(this, restoredState.Procurement);
        _storeGrowth = new StoreGrowthService(
            this,
            restoredState.Promotions?.Select(promotion => new StorePromotionState(
                promotion.StoreId,
                promotion.CampaignId,
                promotion.RemainingMinutes)));
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

    public void AdvanceProcurementMinute(int costModifierPermille = 1_000)
    {
        if (costModifierPermille is < 100 or > 3_000)
        {
            throw new ArgumentOutOfRangeException(nameof(costModifierPermille));
        }

        lock (_gate)
        {
            _procurement.AdvanceMinute(costModifierPermille);
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
            var unlocked = _business.Player.Level == previousLevel
                ? []
                : RegisterUnlockedProductsForAllStores();
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
                ConfigureDefaultAutomaticStocking(id.Value);
            }

            return result;
        }
    }

    public OpenShopResult OpenStore(ShopDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (_gate)
        {
            if (_shopDefinitions.ContainsKey(definition.Id.Value))
            {
                return new OpenShopResult(
                    OpenShopStatus.AlreadyOpen,
                    definition.Id,
                    definition.OpeningCost);
            }

            var result = _business.TryOpenStore(definition);
            if (result.Status == OpenShopStatus.Success)
            {
                _shopDefinitions.Add(definition.Id.Value, definition);
                RegisterUnlockedProducts(_business.GetShop(definition.Id));
                ConfigureDefaultAutomaticStocking(definition.Id.Value);
            }

            return result;
        }
    }

    public StoreGrowthCommandResult UpgradeStore(string shopId, StoreUpgradeKind kind)
    {
        lock (_gate)
        {
            return _storeGrowth.UpgradeStore(shopId, kind);
        }
    }

    public StoreGrowthCommandResult StartPromotion(string shopId, string campaignId)
    {
        lock (_gate)
        {
            return _storeGrowth.StartPromotion(shopId, campaignId);
        }
    }

    public void AdvanceStoreGrowthMinute()
    {
        lock (_gate)
        {
            _storeGrowth.AdvanceMinute();
        }
    }

    public void AdvanceStoreGrowthMinutes(int minutes)
    {
        lock (_gate)
        {
            _storeGrowth.AdvanceMinutes(minutes);
        }
    }

    public StoreGrowthSnapshot GetStoreGrowthSnapshot(string shopId)
    {
        lock (_gate)
        {
            return _storeGrowth.GetSnapshot(shopId);
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

    public IReadOnlyList<StoreCatalogItemSnapshot> GetStoreCatalogSnapshot()
    {
        lock (_gate)
        {
            var openStoreIds = _business.StoreIds
                .Select(id => id.Value)
                .ToHashSet(StringComparer.Ordinal);
            return Array.AsReadOnly(_shopDefinitions.Values
                .OrderBy(definition => definition.StreetOrdinal)
                .ThenBy(definition => definition.Id.Value, StringComparer.Ordinal)
                .Select(definition => new StoreCatalogItemSnapshot(
                    definition.Id.Value,
                    definition.Name,
                    RequiredPlayerLevel: 0,
                    definition.OpeningCost.Cents,
                    openStoreIds.Contains(definition.Id.Value),
                    definition.BrandId.Value,
                    definition.FormatId.Value,
                    definition.StreetOrdinal))
                .ToArray());
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

    public bool IsStoreOpen(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return false;
        }

        lock (_gate)
        {
            return _business.StoreIds.Contains(new ShopId(storeId));
        }
    }

    public bool TryDebitEmployeeExpense(Money amount)
    {
        lock (_gate)
        {
            return _business.TryPayOperatingExpense(amount);
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
                        .ToArray()),
                    store.OperatingCostCents,
                    new StoreDevelopmentSaveData(
                        store.Growth!.ExpansionLevel,
                        store.Growth.ShelfLevel,
                        store.Growth.DecorationLevel),
                    store.Name,
                    store.StoreBrandId,
                    store.StoreFormatId,
                    store.StreetOrdinal))
                .ToArray();
            var promotions = _storeGrowth.CaptureState()
                .Select(state => new StorePromotionSaveData(
                    state.StoreId,
                    state.CampaignId,
                    state.RemainingMinutes))
                .ToArray();
            return new BusinessSaveData(
                snapshot.TotalExperience,
                snapshot.CashCents,
                Array.AsReadOnly(stores),
                _procurement.CaptureSaveData(),
                Array.AsReadOnly(promotions));
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
        var shopId = _business.StoreIds.Single(id => ReferenceEquals(_business.GetShop(id), shop));
        var storeDefinition = _shopDefinitions[shopId.Value];
        var format = CreateFormatEconomics(storeDefinition.FormatId.Value);
        foreach (var definition in GetProductAssortment(storeDefinition))
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
                ScaleCapacity(definition.Capacity, format.InventoryCapacityPermille));
            unlocked?.Add(definition.Id);
        }
    }

    private IEnumerable<ProductDefinition> SelectProductAssortment(ShopDefinition store)
    {
        if (_productDefinitions.Length <= MaximumBaseProductAssortment)
        {
            return _productDefinitions;
        }

        var ranked = _productDefinitions
            .OrderBy(definition => StableProductRank(store.BrandId.Value, definition.Id))
            .ThenBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();
        var selected = new List<ProductDefinition>(MaximumBaseProductAssortment);
        selected.AddRange(ranked
            .Where(definition => definition.RequiredPlayerLevel == 1)
            .Take(Math.Min(4, ranked.Count(definition => definition.RequiredPlayerLevel == 1))));

        var targetCategoryCount = Math.Min(
            6,
            ranked.Select(definition => definition.CategoryId).Distinct(StringComparer.Ordinal).Count());
        foreach (var definition in ranked)
        {
            if (selected.Count == MaximumBaseProductAssortment
                || selected.Select(item => item.CategoryId).Distinct(StringComparer.Ordinal).Count() >= targetCategoryCount)
            {
                break;
            }

            if (!selected.Contains(definition)
                && selected.All(item => !string.Equals(item.CategoryId, definition.CategoryId, StringComparison.Ordinal)))
            {
                selected.Add(definition);
            }
        }

        selected.AddRange(ranked
            .Where(definition => !selected.Contains(definition))
            .Take(MaximumBaseProductAssortment - selected.Count));
        return selected
            .OrderBy(definition => definition.RequiredPlayerLevel)
            .ThenBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<ProductDefinition> GetProductAssortment(ShopDefinition store)
    {
        if (_assortmentsByStore.TryGetValue(store.Id.Value, out var assortment))
        {
            return assortment;
        }

        assortment = SelectProductAssortment(store).ToArray();
        _assortmentsByStore.Add(store.Id.Value, assortment);
        return assortment;
    }

    private static ulong StableProductRank(string brandId, string productId)
    {
        var hash = 14_695_981_039_346_656_037UL;
        foreach (var character in brandId.Concat("|").Concat(productId))
        {
            hash ^= character;
            hash *= 1_099_511_628_211UL;
        }

        return hash;
    }

    private static int ScaleCapacity(int capacity, int permille) =>
        checked(Math.Max(1, (int)((long)capacity * permille / 1_000L)));

    private BusinessStoreSnapshot CreateStoreSnapshot(ShopId shopId)
    {
        var shop = _business.GetShop(shopId);
        var products = GetProductAssortment(_shopDefinitions[shopId.Value])
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
            shop.TotalNetProfit.Cents,
            shop.TotalOperatingCost.Cents,
            _storeGrowth.GetSnapshot(shopId.Value),
            _shopDefinitions[shopId.Value].BrandId.Value,
            _shopDefinitions[shopId.Value].FormatId.Value,
            _shopDefinitions[shopId.Value].StreetOrdinal,
            CreateFormatEconomics(_shopDefinitions[shopId.Value].FormatId.Value),
            GetFacadeStyleKey(_shopDefinitions[shopId.Value].BrandId.Value));
    }

    private StoreFormatEconomicsSnapshot CreateFormatEconomics(string formatId)
    {
        if (!_storeFormats.TryGetValue(formatId, out var format))
        {
            return StoreFormatEconomicsSnapshot.Neutral;
        }

        return new StoreFormatEconomicsSnapshot(
            new DemandSensitivity(
                format.BaseDemandPermille,
                format.PriceSensitivityPermille,
                format.ServiceSensitivityPermille,
                format.QueueSensitivityPermille,
                format.CleanlinessSensitivityPermille),
            format.TimeProfile switch
            {
                "steady" => DemandTimeCurve.Steady,
                "all-day-volume" => DemandTimeCurve.AllDayVolume,
                "afternoon-select" => DemandTimeCurve.AfternoonSelect,
                "commuter-peaks" => DemandTimeCurve.CommuterPeaks,
                _ => throw new InvalidOperationException(
                    $"Unknown store time profile '{format.TimeProfile}'.")
            },
            format.InventoryCapacityPermille,
            format.ProductShelfWeights);
    }

    private static IReadOnlyDictionary<string, StoreFormatDefinition> CreateStoreFormatIndex(
        StoreContentCatalog? content) =>
        content is null
            ? new Dictionary<string, StoreFormatDefinition>(StringComparer.Ordinal)
            : content.Formats.ToDictionary(item => item.Id, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, StoreBrandDefinition> CreateStoreBrandIndex(
        StoreContentCatalog? content) =>
        content is null
            ? new Dictionary<string, StoreBrandDefinition>(StringComparer.Ordinal)
            : content.Brands.ToDictionary(item => item.Id, StringComparer.Ordinal);

    private string GetFacadeStyleKey(string brandId) =>
        _storeBrands.TryGetValue(brandId, out var brand)
            ? brand.FacadeStyleKey
            : "facade-convenience-a";

    private void ConfigureDefaultAutomaticStocking(string storeId)
    {
        var store = CreateStoreSnapshot(new ShopId(storeId));
        var (pricing, stocking) = _storeFormats.TryGetValue(store.StoreFormatId, out var format)
            ? (format.RecommendedPricing, format.RecommendedStocking)
            : (StorePricingPreset.Balanced, StoreStockingPreset.Balanced);
        var plan = StoreStrategyPlanner.Create(
            store,
            pricing,
            stocking);
        foreach (var product in plan.Products)
        {
            var priceResult = ChangePrice(storeId, product.ProductId, product.SalePriceCents);
            if (priceResult.Status != PriceChangeStatus.Success)
            {
                throw new InvalidOperationException(
                    $"Default strategy price failed for product '{product.ProductId}': {priceResult.Status}.");
            }

            _procurement.ConfigureAutoRestock(new AutoRestockPolicy(
                storeId,
                product.ProductId,
                IsEnabled: true,
                product.ReorderPoint,
                product.TargetQuantity,
                product.PreferredChannelId,
                product.UseEmergencySupplierWhenOutOfStock));
        }
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
            definition.InitialSalePriceCents,
            DemandWeightPermille: 1_000,
            definition.CategoryId,
            definition.IconKey,
            definition.RegionTags);
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

    StoreDevelopment? IStoreGrowthGateway.FindDevelopment(string storeId)
    {
        var shopId = new ShopId(storeId);
        return _business.StoreIds.Contains(shopId)
            ? _business.GetShop(shopId).Development
            : null;
    }

    StoreUpgradeResult IStoreGrowthGateway.TryUpgradeStore(
        string storeId,
        StoreUpgradeKind kind) =>
        _business.TryUpgradeStore(new ShopId(storeId), kind);

    bool IStoreGrowthGateway.TryChargePromotion(string storeId, Money cost) =>
        _business.TryPayStorePromotion(new ShopId(storeId), cost);
}
