namespace HajimaoDesktopShop.Application.Simulation;

public interface IRandomSource
{
    double NextDouble();

    int Next(int exclusiveMax);
}
