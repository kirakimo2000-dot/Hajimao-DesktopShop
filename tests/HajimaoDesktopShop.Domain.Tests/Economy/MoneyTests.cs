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
