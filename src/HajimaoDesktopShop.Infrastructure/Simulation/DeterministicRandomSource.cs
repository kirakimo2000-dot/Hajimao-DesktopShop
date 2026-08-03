using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Infrastructure.Simulation;

public sealed class DeterministicRandomSource : IStatefulRandomSource
{
    private const ulong Multiplier = 2_685_821_657_736_338_717UL;
    private const ulong SeedIncrement = 0x9E3779B97F4A7C15UL;
    private readonly object _gate = new();
    private ulong _state;

    public DeterministicRandomSource(int seed)
    {
        _state = MixSeed(unchecked((ulong)(long)seed));
    }

    private DeterministicRandomSource(ulong state)
    {
        RestoreState(state);
    }

    public ulong State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public static DeterministicRandomSource FromState(ulong state) => new(state);

    public double NextDouble()
    {
        lock (_gate)
        {
            return (NextUInt64Core() >> 11) * (1d / 9_007_199_254_740_992d);
        }
    }

    public int Next(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        }

        lock (_gate)
        {
            var bound = (ulong)exclusiveMax;
            var threshold = unchecked(0UL - bound) % bound;
            ulong value;
            do
            {
                value = NextUInt64Core();
            }
            while (value < threshold);

            return checked((int)(value % bound));
        }
    }

    public void RestoreState(ulong state)
    {
        if (state == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        lock (_gate)
        {
            _state = state;
        }
    }

    private ulong NextUInt64Core()
    {
        var value = _state;
        value ^= value >> 12;
        value ^= value << 25;
        value ^= value >> 27;
        _state = value;
        return value * Multiplier;
    }

    private static ulong MixSeed(ulong seed)
    {
        var value = seed + SeedIncrement;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return value == 0 ? SeedIncrement : value;
    }
}
