using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Inventory;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Tests.Inventory;

public sealed class InventorySlotTests
{
    [Fact]
    public void RestockAndRemove_EnforceCapacityAndAvailableStock()
    {
        var product = new Product(
            new ProductId("water"),
            "矿泉水",
            Money.FromYuan(1m),
            Money.FromYuan(2m));
        var slot = new InventorySlot(product, capacity: 10);

        Assert.Equal(StockChangeStatus.Success, slot.Restock(8));
        Assert.Equal(StockChangeStatus.CapacityExceeded, slot.Restock(3));
        Assert.Equal(8, slot.Quantity);
        Assert.Equal(StockChangeStatus.InsufficientStock, slot.Remove(9));
        Assert.Equal(StockChangeStatus.Success, slot.Remove(2));
        Assert.Equal(6, slot.Quantity);
    }
}
