using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Infrastructure.Tests.Simulation;

public sealed class SeededRandomSourceTests
{
    [Fact]
    public void SameSeed_ProducesSameSequenceWithinRequestedRanges()
    {
        var first = new SeededRandomSource(42);
        var second = new SeededRandomSource(42);

        var firstDoubles = Enumerable.Range(0, 10).Select(_ => first.NextDouble()).ToArray();
        var secondDoubles = Enumerable.Range(0, 10).Select(_ => second.NextDouble()).ToArray();
        var firstIntegers = Enumerable.Range(0, 10).Select(_ => first.Next(7)).ToArray();
        var secondIntegers = Enumerable.Range(0, 10).Select(_ => second.Next(7)).ToArray();

        Assert.Equal(firstDoubles, secondDoubles);
        Assert.Equal(firstIntegers, secondIntegers);
        Assert.All(firstDoubles, value => Assert.InRange(value, 0d, 0.9999999999999999d));
        Assert.All(firstIntegers, value => Assert.InRange(value, 0, 6));
    }
}
