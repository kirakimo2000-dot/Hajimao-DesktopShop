# Core Economy Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立可供模拟引擎和 WPF 界面调用的商品、库存、进货、销售、现金和账本经营闭环。

**Architecture:** 所有经营不变量保留在 Domain；`Shop` 聚合原子更新现金、库存和账本。UI 文本、SQLite、SkiaSharp 和时间调度均不进入本计划。

**Tech Stack:** .NET 10, C# 14, xUnit

---

### Task 1: Money value object

**Files:**
- Create: `tests/HajimaoDesktopShop.Domain.Tests/Economy/MoneyTests.cs`
- Create: `src/HajimaoDesktopShop.Domain/Economy/Money.cs`

- [x] **Step 1: Write the failing tests**

```csharp
using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Tests.Economy;

public sealed class MoneyTests
{
    [Fact]
    public void FromYuan_RoundsToNearestCentAwayFromZero()
    {
        Assert.Equal(1_236, Money.FromYuan(12.355m).Cents);
        Assert.Equal(-1_236, Money.FromYuan(-12.355m).Cents);
    }

    [Fact]
    public void Arithmetic_UsesCheckedCentValues()
    {
        var price = Money.FromYuan(12m);

        Assert.Equal(Money.FromYuan(36m), price * 3);
        Assert.Equal(Money.FromYuan(29m), Money.FromYuan(36m) - Money.FromYuan(7m));
    }
}
```

- [x] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter FullyQualifiedName~MoneyTests`

Expected: FAIL because `Money` does not exist.

- [x] **Step 3: Implement Money**

```csharp
namespace HajimaoDesktopShop.Domain.Economy;

public readonly record struct Money(long Cents) : IComparable<Money>
{
    public static Money Zero => new(0);
    public decimal Yuan => Cents / 100m;
    public bool IsPositive => Cents > 0;

    public static Money FromYuan(decimal yuan) =>
        new(checked((long)decimal.Round(yuan * 100m, 0, MidpointRounding.AwayFromZero)));

    public static Money operator +(Money left, Money right) =>
        new(checked(left.Cents + right.Cents));

    public static Money operator -(Money left, Money right) =>
        new(checked(left.Cents - right.Cents));

    public static Money operator *(Money value, int quantity) =>
        new(checked(value.Cents * quantity));

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);
    public override string ToString() => $"¥{Yuan:0.00}";
}
```

- [x] **Step 4: Run GREEN**

Run: `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter FullyQualifiedName~MoneyTests`

Expected: 2 passed, 0 failed.

### Task 2: Product and inventory slot

**Files:**
- Create: `tests/HajimaoDesktopShop.Domain.Tests/Products/ProductTests.cs`
- Create: `tests/HajimaoDesktopShop.Domain.Tests/Inventory/InventorySlotTests.cs`
- Create: `src/HajimaoDesktopShop.Domain/Products/ProductId.cs`
- Create: `src/HajimaoDesktopShop.Domain/Products/Product.cs`
- Create: `src/HajimaoDesktopShop.Domain/Inventory/StockChangeStatus.cs`
- Create: `src/HajimaoDesktopShop.Domain/Inventory/InventorySlot.cs`

- [x] **Step 1: Write Product and InventorySlot tests**

```csharp
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Tests.Products;

public sealed class ProductTests
{
    [Fact]
    public void ChangeSalePrice_UpdatesPositivePrice()
    {
        var product = CreateProduct();
        product.ChangeSalePrice(Money.FromYuan(2.5m));
        Assert.Equal(Money.FromYuan(2.5m), product.SalePrice);
    }

    [Fact]
    public void Constructor_RejectsInvalidIdentityNameAndPrices()
    {
        Assert.Throws<ArgumentException>(() => new ProductId(" "));
        Assert.Throws<ArgumentException>(() => new Product(new ProductId("water"), " ", Money.FromYuan(1m), Money.FromYuan(2m)));
        Assert.Throws<ArgumentException>(() => new Product(new ProductId("water"), "矿泉水", Money.Zero, Money.FromYuan(2m)));
        Assert.Throws<ArgumentException>(() => new Product(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.Zero));
    }

    private static Product CreateProduct() =>
        new(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.FromYuan(2m));
}
```

```csharp
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Inventory;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Tests.Inventory;

public sealed class InventorySlotTests
{
    [Fact]
    public void RestockAndRemove_EnforceCapacityAndAvailableStock()
    {
        var product = new Product(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.FromYuan(2m));
        var slot = new InventorySlot(product, capacity: 10);

        Assert.Equal(StockChangeStatus.Success, slot.Restock(8));
        Assert.Equal(StockChangeStatus.CapacityExceeded, slot.Restock(3));
        Assert.Equal(8, slot.Quantity);
        Assert.Equal(StockChangeStatus.InsufficientStock, slot.Remove(9));
        Assert.Equal(StockChangeStatus.Success, slot.Remove(2));
        Assert.Equal(6, slot.Quantity);
    }
}
```

- [x] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter "FullyQualifiedName~ProductTests|FullyQualifiedName~InventorySlotTests"`

Expected: FAIL because the types do not exist.

- [x] **Step 3: Implement focused domain types**

```csharp
public readonly record struct ProductId
{
    public ProductId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Product ID is required.", nameof(value));
        Value = value.Trim();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed class Product
{
    public Product(ProductId id, string name, Money wholesalePrice, Money salePrice)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
        if (!wholesalePrice.IsPositive) throw new ArgumentException("Wholesale price must be positive.", nameof(wholesalePrice));
        if (!salePrice.IsPositive) throw new ArgumentException("Sale price must be positive.", nameof(salePrice));
        Id = id;
        Name = name.Trim();
        WholesalePrice = wholesalePrice;
        SalePrice = salePrice;
    }

    public ProductId Id { get; }
    public string Name { get; }
    public Money WholesalePrice { get; }
    public Money SalePrice { get; private set; }

    public void ChangeSalePrice(Money price)
    {
        if (!price.IsPositive) throw new ArgumentException("Sale price must be positive.", nameof(price));
        SalePrice = price;
    }
}

public enum StockChangeStatus { Success, InvalidQuantity, CapacityExceeded, InsufficientStock }

public sealed class InventorySlot
{
    public InventorySlot(Product product, int capacity)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Product = product;
        Capacity = capacity;
    }

    public Product Product { get; }
    public int Quantity { get; private set; }
    public int Capacity { get; }

    public StockChangeStatus Restock(int quantity)
    {
        if (quantity <= 0) return StockChangeStatus.InvalidQuantity;
        if (Quantity + quantity > Capacity) return StockChangeStatus.CapacityExceeded;
        Quantity += quantity;
        return StockChangeStatus.Success;
    }

    public StockChangeStatus Remove(int quantity)
    {
        if (quantity <= 0) return StockChangeStatus.InvalidQuantity;
        if (quantity > Quantity) return StockChangeStatus.InsufficientStock;
        Quantity -= quantity;
        return StockChangeStatus.Success;
    }
}
```

- [x] **Step 4: Run GREEN**

Run: `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter "FullyQualifiedName~ProductTests|FullyQualifiedName~InventorySlotTests"`

Expected: all selected tests pass.

### Task 3: Shop transaction aggregate

**Files:**
- Create: `tests/HajimaoDesktopShop.Domain.Tests/Shops/ShopTests.cs`
- Create: `src/HajimaoDesktopShop.Domain/Economy/LedgerEntry.cs`
- Create: `src/HajimaoDesktopShop.Domain/Economy/LedgerEntryType.cs`
- Create: `src/HajimaoDesktopShop.Domain/Shops/StockPurchaseResult.cs`
- Create: `src/HajimaoDesktopShop.Domain/Shops/SaleResult.cs`
- Create: `src/HajimaoDesktopShop.Domain/Shops/Shop.cs`

- [x] **Step 1: Write transaction tests**

```csharp
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Domain.Tests.Shops;

public sealed class ShopTests
{
    [Fact]
    public void PurchaseThenSell_UpdatesCashStockAndLedgerAtomically()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        var purchase = shop.TryPurchaseStock(water.Id, 10);
        Assert.Equal(StockPurchaseStatus.Success, purchase.Status);
        Assert.Equal(Money.FromYuan(90m), shop.Cash);
        Assert.Equal(10, shop.GetInventory(water.Id).Quantity);

        var sale = shop.TrySell(water.Id, 3);
        Assert.Equal(SaleStatus.Success, sale.Status);
        Assert.Equal(Money.FromYuan(6m), sale.Revenue);
        Assert.Equal(Money.FromYuan(3m), sale.GrossProfit);
        Assert.Equal(Money.FromYuan(96m), shop.Cash);
        Assert.Equal(7, shop.GetInventory(water.Id).Quantity);
        Assert.Collection(shop.Ledger,
            entry => Assert.Equal(LedgerEntryType.OpeningBalance, entry.Type),
            entry => Assert.Equal(LedgerEntryType.StockPurchase, entry.Type),
            entry => Assert.Equal(LedgerEntryType.Sale, entry.Type));
    }

    [Fact]
    public void Purchase_WithInsufficientFunds_DoesNotMutateState()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(5m));

        var result = shop.TryPurchaseStock(water.Id, 10);

        Assert.Equal(StockPurchaseStatus.InsufficientFunds, result.Status);
        Assert.Equal(Money.FromYuan(5m), shop.Cash);
        Assert.Equal(0, shop.GetInventory(water.Id).Quantity);
        Assert.Single(shop.Ledger);
    }

    [Fact]
    public void Sale_WithInsufficientStock_DoesNotMutateState()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));
        shop.TryPurchaseStock(water.Id, 1);

        var result = shop.TrySell(water.Id, 2);

        Assert.Equal(SaleStatus.InsufficientStock, result.Status);
        Assert.Equal(Money.FromYuan(99m), shop.Cash);
        Assert.Equal(1, shop.GetInventory(water.Id).Quantity);
        Assert.Equal(2, shop.Ledger.Count);
    }

    private static Product CreateWater() =>
        new(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.FromYuan(2m));

    private static Shop CreateShop(Product product, Money cash)
    {
        var shop = new Shop(cash);
        shop.RegisterProduct(product, capacity: 20);
        return shop;
    }
}
```

- [x] **Step 2: Run RED**

Run: `dotnet test tests/HajimaoDesktopShop.Domain.Tests --filter FullyQualifiedName~ShopTests`

Expected: FAIL because `Shop` and result types do not exist.

- [x] **Step 3: Implement atomic transaction behavior**

```csharp
namespace HajimaoDesktopShop.Domain.Economy;

public enum LedgerEntryType { OpeningBalance, StockPurchase, Sale }
```

```csharp
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Economy;

public sealed record LedgerEntry(
    long Sequence,
    LedgerEntryType Type,
    ProductId? ProductId,
    int Quantity,
    Money Amount,
    Money BalanceAfter);
```

```csharp
using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum StockPurchaseStatus { Success, UnknownProduct, InvalidQuantity, CapacityExceeded, InsufficientFunds }
public readonly record struct StockPurchaseResult(StockPurchaseStatus Status, Money TotalCost);
```

```csharp
using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum SaleStatus { Success, UnknownProduct, InvalidQuantity, InsufficientStock }
public readonly record struct SaleResult(SaleStatus Status, Money Revenue, Money GrossProfit);
```

```csharp
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Inventory;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed class Shop
{
    private readonly Dictionary<ProductId, InventorySlot> _inventory = [];
    private readonly List<LedgerEntry> _ledger = [];

    public Shop(Money openingCash)
    {
        if (openingCash.Cents < 0) throw new ArgumentOutOfRangeException(nameof(openingCash));
        Cash = openingCash;
        AddLedger(LedgerEntryType.OpeningBalance, null, 0, openingCash);
    }

    public Money Cash { get; private set; }
    public IReadOnlyList<LedgerEntry> Ledger => _ledger;

    public void RegisterProduct(Product product, int capacity)
    {
        ArgumentNullException.ThrowIfNull(product);
        _inventory.Add(product.Id, new InventorySlot(product, capacity));
    }

    public InventorySlot GetInventory(ProductId productId) => _inventory[productId];

    public StockPurchaseResult TryPurchaseStock(ProductId productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var slot))
            return new(StockPurchaseStatus.UnknownProduct, Money.Zero);
        if (quantity <= 0)
            return new(StockPurchaseStatus.InvalidQuantity, Money.Zero);
        if (slot.Quantity + quantity > slot.Capacity)
            return new(StockPurchaseStatus.CapacityExceeded, Money.Zero);

        var totalCost = slot.Product.WholesalePrice * quantity;
        if (Cash.Cents < totalCost.Cents)
            return new(StockPurchaseStatus.InsufficientFunds, totalCost);

        slot.Restock(quantity);
        Cash -= totalCost;
        AddLedger(LedgerEntryType.StockPurchase, productId, quantity, new Money(-totalCost.Cents));
        return new(StockPurchaseStatus.Success, totalCost);
    }

    public SaleResult TrySell(ProductId productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var slot))
            return new(SaleStatus.UnknownProduct, Money.Zero, Money.Zero);
        if (quantity <= 0)
            return new(SaleStatus.InvalidQuantity, Money.Zero, Money.Zero);
        if (slot.Quantity < quantity)
            return new(SaleStatus.InsufficientStock, Money.Zero, Money.Zero);

        var revenue = slot.Product.SalePrice * quantity;
        var grossProfit = (slot.Product.SalePrice - slot.Product.WholesalePrice) * quantity;
        slot.Remove(quantity);
        Cash += revenue;
        AddLedger(LedgerEntryType.Sale, productId, quantity, revenue);
        return new(SaleStatus.Success, revenue, grossProfit);
    }

    private void AddLedger(LedgerEntryType type, ProductId? productId, int quantity, Money amount) =>
        _ledger.Add(new LedgerEntry(_ledger.Count + 1L, type, productId, quantity, amount, Cash));
}
```

- [x] **Step 4: Run GREEN and full regression**

Run:

```powershell
dotnet test tests/HajimaoDesktopShop.Domain.Tests
dotnet build HajimaoDesktopShop.slnx
```

Expected: all tests pass; build reports 0 warnings and 0 errors.

### Task 4: Record the version checkpoint

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `progress.md`
- Modify: `task_plan.md`

- [ ] **Step 1: Update progress evidence**

Record RED and GREEN commands, test counts, files, decisions, and any errors in `progress.md`.

- [ ] **Step 2: Update version documentation**

Add the completed domain behavior under `Unreleased` in `CHANGELOG.md`. Mark Phase 1 complete and Phase 2 in progress in `task_plan.md`.
