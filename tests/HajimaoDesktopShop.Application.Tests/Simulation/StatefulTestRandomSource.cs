using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

internal sealed class StatefulTestRandomSource(ulong state) : IStatefulRandomSource
{
    private ulong _state = state == 0 ? 1 : state;

    public ulong State => _state;

    public double NextDouble() => (NextValue() >> 11) * (1d / 9_007_199_254_740_992d);

    public int Next(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        }

        return (int)(NextValue() % (ulong)exclusiveMax);
    }

    public void RestoreState(ulong restoredState)
    {
        if (restoredState == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restoredState));
        }

        _state = restoredState;
    }

    private ulong NextValue()
    {
        _state = unchecked((_state * 6_364_136_223_846_793_005UL) + 1_442_695_040_888_963_407UL);
        return _state;
    }
}
