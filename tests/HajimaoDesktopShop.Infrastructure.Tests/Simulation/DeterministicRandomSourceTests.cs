using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Infrastructure.Tests.Simulation;

public sealed class DeterministicRandomSourceTests
{
    [Fact]
    public void SameSeed_ProducesSameMixedSequence()
    {
        var first = new DeterministicRandomSource(42);
        var second = new DeterministicRandomSource(42);

        Assert.Equal(first.NextDouble(), second.NextDouble());
        Assert.Equal(first.Next(17), second.Next(17));
        Assert.Equal(first.NextDouble(), second.NextDouble());
        Assert.Equal(first.Next(10_000), second.Next(10_000));
    }

    [Fact]
    public void FromState_ContinuesAtTheExactNextValue()
    {
        var source = new DeterministicRandomSource(42);
        _ = source.NextDouble();
        var state = source.State;
        var expected = new[] { source.Next(1_000), source.Next(1_000), source.Next(1_000) };

        var restored = DeterministicRandomSource.FromState(state);

        Assert.Equal(expected, new[] { restored.Next(1_000), restored.Next(1_000), restored.Next(1_000) });
    }

    [Fact]
    public void InvalidBoundsAndState_AreRejected()
    {
        var source = new DeterministicRandomSource(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.RestoreState(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DeterministicRandomSource.FromState(0));
    }
}
