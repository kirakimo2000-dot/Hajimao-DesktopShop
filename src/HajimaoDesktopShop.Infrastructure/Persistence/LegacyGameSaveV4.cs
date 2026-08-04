using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV4(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business,
    BusinessSimulationSaveData? BusinessSimulation)
{
    public GameSaveData UpgradeToV5()
    {
        var simulation = BusinessSimulation is null
            ? null
            : BusinessSimulation with
            {
                Employees = BusinessSimulation.Employees
                    .Select(employee => employee with
                    {
                        TrainingLevel = 0,
                        EnergyPermille = 1_000,
                        SatisfactionPermille = 700,
                        WorkMinutesTowardSatisfactionLoss = 0,
                        RestMinutesTowardSatisfactionGain = 0,
                        ShiftStartMinute = 0,
                        ShiftEndMinute = 0,
                        IsAlwaysOn = true
                    })
                    .ToArray(),
                EmployeeOperations = null
            };
        return new GameSaveData(
            5,
            SavedAtUtc,
            Shop,
            Simulation,
            Business,
            simulation);
    }
}
