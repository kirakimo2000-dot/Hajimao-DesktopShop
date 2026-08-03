using System.Diagnostics;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Offline;

public sealed class OfflineSettlementServiceTests
{
    private static readonly DateTimeOffset SavedAt =
        new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Settle_TruncatesFractionsCapsElapsedTimeAndProtectsClockRollback()
    {
        var exact = OfflineSettlementService.Settle(
            CreateSimulation().Simulation,
            SavedAt,
            SavedAt.AddSeconds(12).AddMilliseconds(999));
        var capped = OfflineSettlementService.Settle(
            CreateSimulation().Simulation,
            SavedAt,
            SavedAt.AddHours(24),
            new OfflineSettlementPolicy(maxOfflineSeconds: 5));
        var rollback = OfflineSettlementService.Settle(
            CreateSimulation().Simulation,
            SavedAt,
            SavedAt.AddMinutes(-1));

        Assert.Equal(12, exact.AppliedSeconds);
        Assert.False(exact.WasCapped);
        Assert.Equal(5, capped.AppliedSeconds);
        Assert.True(capped.WasCapped);
        Assert.Equal(0, rollback.AppliedSeconds);
        Assert.Equal(OfflineTimeAnomaly.ClockMovedBackward, rollback.Anomaly);
        Assert.Throws<ArgumentOutOfRangeException>(() => new OfflineSettlementPolicy(0));
    }

    [Fact]
    public void OfflineSettlement_IsIdenticalToOnlineAdvanceFromTheSameSave()
    {
        var initial = CreateSimulation(withStaff: true);
        initial.Simulation.AdvanceRealSeconds(37);
        var businessSave = initial.Service.CaptureSaveData();
        var simulationSave = initial.Simulation.CaptureSaveData();
        var online = RestoreSimulation(businessSave, simulationSave);
        var offline = RestoreSimulation(businessSave, simulationSave);

        online.Simulation.AdvanceRealSeconds(3_600);
        var result = OfflineSettlementService.Settle(
            offline.Simulation,
            SavedAt,
            SavedAt.AddHours(1));

        Assert.Equal(3_600, result.AppliedSeconds);
        Assert.Equivalent(online.Simulation.GetSnapshot(), offline.Simulation.GetSnapshot(), strict: true);
        Assert.Equivalent(online.Simulation.CaptureSaveData(), offline.Simulation.CaptureSaveData(), strict: true);
    }

    [Fact]
    public void DefaultEightHourTwoStoreSettlement_StaysWithinPerformanceBoundary()
    {
        var session = CreateSimulation(openSecondStore: true);
        var stopwatch = Stopwatch.StartNew();

        var result = OfflineSettlementService.Settle(
            session.Simulation,
            SavedAt,
            SavedAt.AddDays(7));

        stopwatch.Stop();
        Assert.Equal(28_800, result.AppliedSeconds);
        Assert.True(result.WasCapped);
        Assert.Equal(28_800, session.Simulation.GetSnapshot().GameMinute);
        Assert.Equal(2, session.Simulation.GetSnapshot().Stores.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"Settlement took {stopwatch.Elapsed}.");
    }

    private static (BusinessGameService Service, BusinessSimulation Simulation) CreateSimulation(
        bool withStaff = false,
        bool openSecondStore = false)
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 50_000);
        if (openSecondStore)
        {
            service.OpenStore("station-store");
            service.PurchaseStock("station-store", "water", 50_000);
        }

        var assignments = withStaff
            ? new[]
            {
                new StoreEmployeeAssignment(
                    "corner-store",
                    new Employee(
                        new EmployeeId("cashier"),
                        "小葵",
                        EmployeeRole.Cashier,
                        1_000,
                        new Money(1_001)))
            }
            : [];
        return (
            service,
            new BusinessSimulation(
                service,
                assignments,
                new StatefulTestRandomSource(77),
                new BusinessSimulationOptions(baseArrivalBasisPoints: 2_500)));
    }

    private static (BusinessGameService Service, BusinessSimulation Simulation) RestoreSimulation(
        BusinessSaveData businessSave,
        BusinessSimulationSaveData simulationSave)
    {
        var service = CreateService(businessSave);
        return (
            service,
            new BusinessSimulation(
                service,
                simulationSave,
                new StatefulTestRandomSource(1),
                new BusinessSimulationOptions(baseArrivalBasisPoints: 2_500)));
    }

    private static BusinessGameService CreateService(BusinessSaveData? restored = null)
    {
        ProductDefinition[] products =
        [
            new("water", "矿泉水", 100, 200, 100_000, "ambient")
        ];
        ShopDefinition[] stores =
        [
            new(new ShopId("corner-store"), "街角店", 1, Money.Zero),
            new(new ShopId("station-store"), "车站店", 1, Money.Zero)
        ];
        var curve = new LevelCurve([0, 100]);
        return restored is null
            ? new BusinessGameService(products, stores, curve, "corner-store", 20_000_000)
            : new BusinessGameService(products, stores, curve, restored);
    }
}
