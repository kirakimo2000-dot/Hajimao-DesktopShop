using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Inventory;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed class Shop
{
    private readonly BusinessWallet _wallet;
    private readonly Dictionary<ProductId, InventorySlot> _inventory = [];
    private readonly List<LedgerEntry> _ledger = [];

    public Shop(Money openingCash)
        : this(
            new BusinessWallet(openingCash),
            new ShopFinancialState(openingCash, Money.Zero, Money.Zero, Money.Zero),
            addOpeningBalance: true)
    {
    }

    private Shop(BusinessWallet wallet, ShopFinancialState state, bool addOpeningBalance)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(state);

        if (state.Cash.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (state.TotalRevenue.Cents < 0 || state.TotalStockPurchaseCost.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        _wallet = wallet;
        TotalRevenue = state.TotalRevenue;
        TotalStockPurchaseCost = state.TotalStockPurchaseCost;
        TotalGrossProfit = state.TotalGrossProfit;
        if (state.TotalWageCost.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        TotalWageCost = state.TotalWageCost;

        if (addOpeningBalance)
        {
            AddLedger(LedgerEntryType.OpeningBalance, null, 0, state.Cash);
        }
    }

    public static Shop Restore(ShopFinancialState state) =>
        new(new BusinessWallet(state.Cash), state, addOpeningBalance: false);

    internal static Shop CreateWithWallet(BusinessWallet wallet) =>
        new(
            wallet,
            new ShopFinancialState(wallet.Balance, Money.Zero, Money.Zero, Money.Zero),
            addOpeningBalance: false);

    internal static Shop RestoreWithWallet(BusinessWallet wallet, ShopFinancialState state) =>
        new(wallet, state with { Cash = wallet.Balance }, addOpeningBalance: false);

    public Money Cash => _wallet.Balance;

    public Money TotalRevenue { get; private set; }

    public Money TotalStockPurchaseCost { get; private set; }

    public Money TotalGrossProfit { get; private set; }

    public Money TotalWageCost { get; private set; }

    public Money TotalNetProfit => TotalGrossProfit - TotalWageCost;

    public IReadOnlyList<LedgerEntry> Ledger => _ledger;

    public bool ContainsProduct(ProductId productId) => _inventory.ContainsKey(productId);

    public void RegisterProduct(Product product, int capacity, int initialQuantity = 0)
    {
        ArgumentNullException.ThrowIfNull(product);
        _inventory.Add(product.Id, new InventorySlot(product, capacity, initialQuantity));
    }

    public InventorySlot GetInventory(ProductId productId) => _inventory[productId];

    public PriceChangeResult TryChangePrice(ProductId productId, Money salePrice)
    {
        if (!_inventory.TryGetValue(productId, out var slot))
        {
            return new PriceChangeResult(PriceChangeStatus.UnknownProduct, Money.Zero);
        }

        if (!salePrice.IsPositive)
        {
            return new PriceChangeResult(PriceChangeStatus.InvalidPrice, slot.Product.SalePrice);
        }

        slot.Product.ChangeSalePrice(salePrice);
        return new PriceChangeResult(PriceChangeStatus.Success, salePrice);
    }

    public StockPurchaseResult TryPurchaseStock(ProductId productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var slot))
        {
            return new StockPurchaseResult(StockPurchaseStatus.UnknownProduct, Money.Zero);
        }

        if (quantity <= 0)
        {
            return new StockPurchaseResult(StockPurchaseStatus.InvalidQuantity, Money.Zero);
        }

        if (slot.Quantity + quantity > slot.Capacity)
        {
            return new StockPurchaseResult(StockPurchaseStatus.CapacityExceeded, Money.Zero);
        }

        var totalCost = slot.Product.WholesalePrice * quantity;
        if (!_wallet.TryDebit(totalCost))
        {
            return new StockPurchaseResult(StockPurchaseStatus.InsufficientFunds, totalCost);
        }

        slot.Restock(quantity);
        TotalStockPurchaseCost += totalCost;
        AddLedger(LedgerEntryType.StockPurchase, productId, quantity, new Money(-totalCost.Cents));
        return new StockPurchaseResult(StockPurchaseStatus.Success, totalCost);
    }

    public SaleResult TrySell(ProductId productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var slot))
        {
            return new SaleResult(SaleStatus.UnknownProduct, Money.Zero, Money.Zero);
        }

        if (quantity <= 0)
        {
            return new SaleResult(SaleStatus.InvalidQuantity, Money.Zero, Money.Zero);
        }

        if (slot.Quantity < quantity)
        {
            return new SaleResult(SaleStatus.InsufficientStock, Money.Zero, Money.Zero);
        }

        var revenue = slot.Product.SalePrice * quantity;
        var grossProfit = (slot.Product.SalePrice - slot.Product.WholesalePrice) * quantity;
        slot.Remove(quantity);
        _wallet.Credit(revenue);
        TotalRevenue += revenue;
        TotalGrossProfit += grossProfit;
        AddLedger(LedgerEntryType.Sale, productId, quantity, revenue);
        return new SaleResult(SaleStatus.Success, revenue, grossProfit);
    }

    internal void RecordWagePayment(Money amount)
    {
        if (amount.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        TotalWageCost += amount;
        AddLedger(LedgerEntryType.WagePayment, null, 1, new Money(-amount.Cents));
    }

    private void AddLedger(
        LedgerEntryType type,
        ProductId? productId,
        int quantity,
        Money amount) =>
        _ledger.Add(new LedgerEntry(_ledger.Count + 1L, type, productId, quantity, amount, Cash));
}
