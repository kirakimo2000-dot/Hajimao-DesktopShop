using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopBusinessSessionFactory
{
    public static DesktopBusinessSessionStartResult Create(
        IReadOnlyList<ProductDefinition> products,
        GameSaveData? save,
        int seed,
        DateTimeOffset nowUtc,
        StoreContentCatalog? storeContent = null)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(products));
        }

        var random = new DeterministicRandomSource(seed);
        if (save is null)
        {
            var newSession = BusinessSession.Create(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                DesktopGameContent.OpeningCashCents,
                DesktopGameContent.CreateStarterAssignments(),
                random,
                DesktopGameContent.SimulationOptions,
                experiencePerItemSold: DesktopGameContent.ExperiencePerItemSold,
                storeContent);
            ConfigureStarterShifts(newSession);
            return new DesktopBusinessSessionStartResult(
                newSession,
                IsNewGame: true);
        }

        var restoredSession = BusinessSession.RestoreOrUpgrade(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                save,
                DesktopGameContent.CreateStarterAssignments(),
                random,
                DesktopGameContent.SimulationOptions,
                experiencePerItemSold: DesktopGameContent.ExperiencePerItemSold,
                storeContent);
        return new DesktopBusinessSessionStartResult(
            restoredSession,
            IsNewGame: false);
    }

    private static void ConfigureStarterShifts(BusinessSession session)
    {
        foreach (var employee in session.Simulation.GetSnapshot().Employees.Employees)
        {
            var result = session.Simulation.Employees.SetShift(
                employee.EmployeeId,
                DesktopGameContent.StarterShiftStartMinute,
                DesktopGameContent.StarterShiftEndMinute);
            if (result.Status != EmployeeCommandStatus.Success)
            {
                throw new InvalidOperationException(
                    $"Starter shift configuration failed for '{employee.EmployeeId}': {result.Status}.");
            }
        }
    }
}
