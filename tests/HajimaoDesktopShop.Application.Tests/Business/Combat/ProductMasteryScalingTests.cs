using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class ProductMasteryScalingTests
{
    [Theory]
    [InlineData(1, 1_000, 1_000)]
    [InlineData(10, 1_315, 1_135)]
    [InlineData(20, 1_665, 1_285)]
    public void MasteryImprovesPowerAndRevenueWithoutExponentialInflation(
        int level,
        int expectedPower,
        int expectedRevenue)
    {
        Assert.Equal(expectedPower, ProductMasteryScaling.PowerPermille(level));
        Assert.Equal(expectedRevenue, ProductMasteryScaling.RevenuePermille(level));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(49, 3)]
    [InlineData(50, 4)]
    [InlineData(250, 5)]
    [InlineData(1_000, 6)]
    public void ServedCustomersAutomaticallyUnlockUpToSixSlots(int served, int slots) =>
        Assert.Equal(slots, ProductSlotProgressionPolicy.SlotsForServedCustomers(served));
}
