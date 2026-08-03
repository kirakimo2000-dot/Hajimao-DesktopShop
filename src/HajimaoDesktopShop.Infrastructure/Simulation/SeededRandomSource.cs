using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Infrastructure.Simulation;

public sealed class SeededRandomSource : IRandomSource
{
    private readonly object _gate = new();
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public double NextDouble()
    {
        lock (_gate)
        {
            return _random.NextDouble();
        }
    }

    public int Next(int exclusiveMax)
    {
        lock (_gate)
        {
            return _random.Next(exclusiveMax);
        }
    }
}
