using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Inventory;

public sealed class InventorySlot
{
    public InventorySlot(Product product, int capacity, int initialQuantity = 0)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (initialQuantity < 0 || initialQuantity > capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity));
        }

        Product = product;
        Capacity = capacity;
        Quantity = initialQuantity;
    }

    public Product Product { get; }

    public int Quantity { get; private set; }

    public int Capacity { get; }

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
