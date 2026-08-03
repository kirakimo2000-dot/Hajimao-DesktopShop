using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

internal sealed class ScriptedRandomSource(params double[] doubles) : IRandomSource
{
    private readonly Queue<double> _doubles = new(doubles);

    public double NextDouble() => _doubles.TryDequeue(out var value) ? value : 1d;

    public int Next(int exclusiveMax) => 0;
}
