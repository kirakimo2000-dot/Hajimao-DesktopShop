using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Inventory;

public sealed class InventorySlot
{
    public InventorySlot(Product product, int capacity, int initialQuantity = 0)
        : this(product, capacity, capacity, initialQuantity)
    {
    }

    internal InventorySlot(
        Product product,
        int baseCapacity,
        int capacity,
        int initialQuantity = 0)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (baseCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseCapacity));
        }

        if (capacity < baseCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (initialQuantity < 0 || initialQuantity > capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity));
        }

        Product = product;
        BaseCapacity = baseCapacity;
        Capacity = capacity;
        Quantity = initialQuantity;
    }

    public Product Product { get; }

    public int Quantity { get; private set; }

    public int BaseCapacity { get; }

    public int Capacity { get; private set; }

    internal void ApplyCapacityPermille(int capacityPermille)
    {
        if (capacityPermille < 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityPermille));
        }

        var capacity = checked((long)BaseCapacity * capacityPermille / 1_000);
        Capacity = checked((int)capacity);
    }

    public StockChangeStatus Restock(int quantity)
    {
        if (quantity <= 0)
        {
            return StockChangeStatus.InvalidQuantity;
        }

        if (Quantity + quantity > Capacity)
        {
            return StockChangeStatus.CapacityExceeded;
        }

        Quantity += quantity;
        return StockChangeStatus.Success;
    }

    public StockChangeStatus Remove(int quantity)
    {
        if (quantity <= 0)
        {
            return StockChangeStatus.InvalidQuantity;
        }

        if (quantity > Quantity)
        {
            return StockChangeStatus.InsufficientStock;
        }

        Quantity -= quantity;
        return StockChangeStatus.Success;
    }
}
