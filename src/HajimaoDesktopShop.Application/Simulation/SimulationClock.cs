namespace HajimaoDesktopShop.Application.Simulation;

public sealed class SimulationClock
{
    public SimulationClock(long gameMinute = 0)
    {
        if (gameMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameMinute));
        }

        GameMinute = gameMinute;
    }

    public long GameMinute { get; private set; }

    public void AdvanceRealSecond(Action processTick)
    {
        ArgumentNullException.ThrowIfNull(processTick);

        processTick();
        GameMinute++;
    }
}
