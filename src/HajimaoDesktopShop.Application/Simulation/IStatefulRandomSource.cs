namespace HajimaoDesktopShop.Application.Simulation;

public interface IStatefulRandomSource : IRandomSource
{
    ulong State { get; }

    void RestoreState(ulong state);
}
