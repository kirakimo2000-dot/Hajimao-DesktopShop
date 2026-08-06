using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

public sealed class BusinessSimulationTests
{
    [Fact]
    public void Advance_ProcessesEveryOpenStoreThenRoutesOneSharedVisitor()
    {
        var service = CreateService();
        var simulation = new BusinessSimulation(
            service,
            [
                AssignCashier("zeta-store", "cashier-z", 1_000),
                AssignCashier("alpha-store", "cashier-a", 1_000)
            ],
            new ScriptedRandomSource(0, 0, 0, 0),
            CreateAlwaysBusyOptions());
        service.OpenStore("alpha-store");
        service.PurchaseStock("zeta-store", "water", 5);
        service.PurchaseStock("alpha-store", "water", 5);

        simulation.AdvanceRealSecond();
        var snapshot = simulation.GetSnapshot();

        Assert.Equal(1, snapshot.GameMinute);
        Assert.Equal(["alpha-store", "zeta-store"], snapshot.Stores.Select(store => store.StoreId));
        Assert.All(snapshot.Stores, store => Assert.Equal(959, store.ServicePermille));
        Assert.Equal(1, snapshot.Stores.Sum(store => store.Visitors));
        Assert.Equal(1, snapshot.Stores.Sum(store => store.CheckoutQueueLength));
    }

    [Fact]
    public void PriceChange_LowersTheStoreArrivalScore()
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 5);
        var simulation = new BusinessSimulation(
            service,
            [AssignCashier("zeta-store", "cashier", 1_000)],
            new ScriptedRandomSource(1, 1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 5_000));

        simulation.AdvanceRealSecond();
        var normal = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand.FinalBasisPoints;
        service.ChangePrice("zeta-store", "water", 400);
        simulation.AdvanceRealSecond();
        var expensive = Assert.Single(simulation.GetSnapshot().Stores).ArrivalDemand.FinalBasisPoints;

        Assert.True(expensive < normal);
    }

    [Fact]
    public void CashierEfficiency_ChangesCheckoutThroughput()
    {
        var slowService = CreateService();
        var fastService = CreateService();
        slowService.PurchaseStock("zeta-store", "water", 10);
        fastService.PurchaseStock("zeta-store", "water", 10);
        var scripted = Enumerable.Repeat(0d, 64).ToArray();
        var slow = new BusinessSimulation(
            slowService,
            [AssignCashier("zeta-store", "slow", 500)],
            new ScriptedRandomSource(scripted),
            CreateAlwaysBusyOptions());
        var fast = new BusinessSimulation(
            fastService,
            [AssignCashier("zeta-store", "fast", 2_000)],
            new ScriptedRandomSource(scripted),
            CreateAlwaysBusyOptions());

        slow.AdvanceRealSeconds(8);
        fast.AdvanceRealSeconds(8);

        Assert.True(
            Assert.Single(fast.GetSnapshot().Stores).CompletedSales
            > Assert.Single(slow.GetSnapshot().Stores).CompletedSales);
    }

    [Fact]
    public void VisitorsReduceCleanlinessAndPaidCleanerRecoversIt()
    {
        var dirtyService = CreateService();
        var cleanService = CreateService();
        dirtyService.PurchaseStock("zeta-store", "water", 2);
        cleanService.PurchaseStock("zeta-store", "water", 2);
        var dirty = new BusinessSimulation(
            dirtyService,
            [],
            new ScriptedRandomSource(0, 0, 1),
            CreateAlwaysBusyOptions());
        var clean = new BusinessSimulation(
            cleanService,
            [AssignCleaner("zeta-store", "cleaner", 1_000)],
            new ScriptedRandomSource(0, 0, 1),
            CreateAlwaysBusyOptions());

        dirty.AdvanceRealSeconds(2);
        clean.AdvanceRealSeconds(2);

        Assert.Equal(988, Assert.Single(dirty.GetSnapshot().Stores).CleanlinessPermille);
        Assert.Equal(997, Assert.Single(clean.GetSnapshot().Stores).CleanlinessPermille);
    }

    [Fact]
    public void MultipleStores_CompeteForAtMostOneSharedStreetVisitorPerMinute()
    {
        var service = CreateService();
        Assert.Equal(OpenShopStatus.Success, service.OpenStore("alpha-store").Status);
        service.PurchaseStock("zeta-store", "water", 2);
        service.PurchaseStock("alpha-store", "water", 2);
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(0, 0),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSecond();

        var stores = simulation.GetSnapshot().Stores;
        Assert.Equal(1, stores.Sum(store => store.Visitors));
        Assert.Equal(1, stores.Count(store => store.Visitors == 1));
        Assert.Equal(2, simulation.GetSnapshot().Street.Stores.Count);
    }

    [Fact]
    public void StreetSnapshot_ChangesWeatherWithoutAddingAPlayerSpeedControl()
    {
        var simulation = new BusinessSimulation(
            CreateService(),
            [],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 8_000));

        simulation.AdvanceRealSeconds(720);

        var snapshot = simulation.GetSnapshot();
        Assert.Equal(StreetWeather.Rain, snapshot.Street.Weather);
        Assert.Equal(6_700, Assert.Single(snapshot.Stores).ArrivalDemand.FinalBasisPoints);
        Assert.Equal(4_690, snapshot.Street.SharedTrafficBasisPoints);
        Assert.Equal(720, snapshot.GameMinute);
    }

    [Fact]
    public void InsufficientWage_SuspendsEmployeeWorkWithoutPartialMutation()
    {
        var service = CreateService(openingCashCents: 100);
        service.PurchaseStock("zeta-store", "water", 1);
        var simulation = new BusinessSimulation(
            service,
            [AssignCashier("zeta-store", "cashier", 1_000, hourlyWageCents: 6_000)],
            new ScriptedRandomSource(0, 0),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSecond();
        var store = Assert.Single(simulation.GetSnapshot().Stores);

        Assert.Equal(1, store.WagePaymentFailures);
        Assert.Equal(0, store.ServicePermille);
        Assert.Equal(0, store.CompletedSales);
        Assert.Equal(1, store.CheckoutQueueLength);
        Assert.Equal(0, Assert.Single(simulation.GetSnapshot().Business.Stores).WageCostCents);
    }

    [Fact]
    public void OffShiftEmployee_DoesNotReceiveWageOrServiceCreditAndRecovers()
    {
        var service = CreateService();
        var employee = CreateEmployee("cashier", EmployeeRole.Cashier, 1_000, 6_000);
        employee.RecordWorkedConditionMinute();
        var simulation = new BusinessSimulation(
            service,
            [new StoreEmployeeAssignment("zeta-store", employee)],
            new ScriptedRandomSource(1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        simulation.Employees.SetShift("cashier", 480, 960);

        simulation.AdvanceRealSecond();
        var store = Assert.Single(simulation.GetSnapshot().Stores);
        var employeeSnapshot = Assert.Single(simulation.Employees.GetSnapshot().Employees);

        Assert.Equal(0, employee.WorkedMinutes);
        Assert.Equal(1_000, employeeSnapshot.EnergyPermille);
        Assert.Equal(0, store.ServicePermille);
        Assert.Equal(0, Assert.Single(simulation.GetSnapshot().Business.Stores).WageCostCents);
    }

    [Fact]
    public void WorkingEmployee_LosesEnergyAndUsesEffectiveEfficiency()
    {
        var service = CreateService();
        var employee = CreateEmployee("cashier", EmployeeRole.Cashier, 1_000, 60);
        var simulation = new BusinessSimulation(
            service,
            [new StoreEmployeeAssignment("zeta-store", employee)],
            new ScriptedRandomSource(1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        simulation.AdvanceRealSecond();
        var store = Assert.Single(simulation.GetSnapshot().Stores);
        var employeeSnapshot = Assert.Single(simulation.Employees.GetSnapshot().Employees);

        Assert.Equal(1, employee.WorkedMinutes);
        Assert.Equal(998, employeeSnapshot.EnergyPermille);
        Assert.Equal(959, employeeSnapshot.EffectiveEfficiencyPermille);
        Assert.Equal(959, store.ServicePermille);
    }

    [Fact]
    public void Snapshot_ExposesCheckoutTargetRemainingTimeAndRolePriorities()
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 10);
        var simulation = new BusinessSimulation(
            service,
            [AssignCashier("zeta-store", "cashier", 1_000)],
            new ScriptedRandomSource(Enumerable.Repeat(0d, 12).ToArray()),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSeconds(2);

        var employee = Assert.Single(simulation.GetSnapshot().Employees.Employees);
        Assert.Equal(EmployeeTaskKind.Checkout, employee.CurrentTask?.Kind);
        Assert.Equal("water", employee.CurrentTask?.TargetKey);
        Assert.Equal("矿泉水", employee.CurrentTask?.TargetName);
        Assert.True(employee.CurrentTask?.RemainingMinutes > 0);
        Assert.Equal(EmployeeTaskKind.Checkout, employee.TaskPriorities?[0]);
    }

    [Fact]
    public void Snapshot_ReportsOffShiftEmployeeAsResting()
    {
        var simulation = new BusinessSimulation(
            CreateService(),
            [AssignCashier("zeta-store", "cashier", 1_000)],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        simulation.Employees.SetShift("cashier", 480, 960);

        var employee = Assert.Single(simulation.GetSnapshot().Employees.Employees);

        Assert.Equal(EmployeeTaskKind.Rest, employee.CurrentTask?.Kind);
        Assert.True(employee.CurrentTask?.IsResting);
    }

    [Fact]
    public void ExhaustedScheduledEmployee_RestsThenReturnsToDuty()
    {
        var exhausted = Employee.Restore(
            new EmployeeId("cashier"),
            "cashier",
            EmployeeRole.Cashier,
            1_000,
            new Money(60),
            new EmployeeWorkState(0, Money.Zero, 0),
            new EmployeeConditionState(0, 0, 700, 0, 0));
        var simulation = new BusinessSimulation(
            CreateService(),
            [new StoreEmployeeAssignment("zeta-store", exhausted)],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        simulation.AdvanceRealSecond();
        var resting = Assert.Single(simulation.GetSnapshot().Employees.Employees);

        Assert.Equal(4, resting.EnergyPermille);
        Assert.Equal(EmployeeTaskKind.Rest, resting.CurrentTask?.Kind);

        simulation.AdvanceRealSecond();
        var working = Assert.Single(simulation.GetSnapshot().Employees.Employees);
        Assert.Equal(2, working.EnergyPermille);
        Assert.Equal(EmployeeTaskKind.CustomerService, working.CurrentTask?.Kind);
    }

    [Fact]
    public void Manager_CoversCheckoutWhenNoCashierIsAssigned()
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 10);
        var simulation = new BusinessSimulation(
            service,
            [AssignRole("zeta-store", "manager", EmployeeRole.Manager, 1_000)],
            new ScriptedRandomSource(Enumerable.Repeat(0d, 12).ToArray()),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSeconds(2);

        var employee = Assert.Single(simulation.GetSnapshot().Employees.Employees);
        Assert.Equal(EmployeeTaskKind.Checkout, employee.CurrentTask?.Kind);
        Assert.Equal("water", employee.CurrentTask?.TargetKey);
    }

    [Fact]
    public void CheckoutDuty_DoesNotAlsoProvideCustomerServiceCredit()
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 10);
        var simulation = new BusinessSimulation(
            service,
            [AssignCashier("zeta-store", "cashier", 1_000)],
            new ScriptedRandomSource(Enumerable.Repeat(0d, 12).ToArray()),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSeconds(2);

        var snapshot = simulation.GetSnapshot();
        Assert.Equal(
            EmployeeTaskKind.Checkout,
            Assert.Single(snapshot.Employees.Employees).CurrentTask?.Kind);
        Assert.Equal(0, Assert.Single(snapshot.Stores).ServicePermille);
    }

    [Fact]
    public void Restocker_TracksNearestInboundProductAndRealRemainingTime()
    {
        var service = CreateService();
        var placed = service.PlaceProcurementOrder(
            "zeta-store",
            "water",
            "regional-distributor",
            10);
        Assert.NotNull(placed.Order);
        var simulation = new BusinessSimulation(
            service,
            [AssignRole("zeta-store", "restocker", EmployeeRole.Restocker, 1_000)],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        simulation.AdvanceRealSecond();

        var order = Assert.Single(service.GetProcurementSnapshot().PendingOrders);
        var employee = Assert.Single(simulation.GetSnapshot().Employees.Employees);
        Assert.Equal(EmployeeTaskKind.Restock, employee.CurrentTask?.Kind);
        Assert.Equal("ambient", employee.CurrentTask?.TargetKey);
        Assert.Equal("矿泉水", employee.CurrentTask?.TargetName);
        Assert.Equal(order.RemainingMinutes, employee.CurrentTask?.RemainingMinutes);
    }

    [Fact]
    public void CleanerRecovery_WithExtremeValidEfficiency_DoesNotOverflow()
    {
        var service = CreateService();
        var simulation = new BusinessSimulation(
            service,
            [AssignCleaner("zeta-store", "cleaner", int.MaxValue)],
            new ScriptedRandomSource(1));

        simulation.AdvanceRealSecond();

        Assert.Equal(1_000, Assert.Single(simulation.GetSnapshot().Stores).CleanlinessPermille);
    }

    [Fact]
    public void CaptureAndRestore_MidDayContinuesWithIdenticalQueuesWagesAndDayReport()
    {
        var service = CreateService();
        service.PurchaseStock("zeta-store", "water", 100);
        var original = new BusinessSimulation(
            service,
            [
                AssignCashier("zeta-store", "cashier", 750, hourlyWageCents: 1_001),
                AssignCleaner("zeta-store", "cleaner", 1_100)
            ],
            new StatefulTestRandomSource(42),
            CreateAlwaysBusyOptions());
        original.AdvanceRealSeconds(123);

        var businessSave = service.CaptureSaveData();
        var simulationSave = original.CaptureSaveData();
        var restoredService = CreateService(businessSave);
        var restored = new BusinessSimulation(
            restoredService,
            simulationSave,
            new StatefulTestRandomSource(1),
            CreateAlwaysBusyOptions());

        original.AdvanceRealSeconds(1_500);
        restored.AdvanceRealSeconds(1_500);

        Assert.Equivalent(original.GetSnapshot(), restored.GetSnapshot(), strict: true);
        Assert.Equivalent(original.CaptureSaveData(), restored.CaptureSaveData(), strict: true);
        Assert.NotNull(restored.GetSnapshot().LastCompletedDay);
    }

    [Fact]
    public void CaptureAndRestore_RoundTripsCandidatesHiredStaffTrainingConditionAndShift()
    {
        var service = CreateService(openingCashCents: 1_000_000);
        var original = new BusinessSimulation(
            service,
            [],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        original.Employees.RefreshCandidates();
        var candidate = original.Employees.GetSnapshot().Candidates[1];
        var hired = original.Employees.Hire(candidate.CandidateId, "zeta-store");
        Assert.Equal(EmployeeCommandStatus.Success, hired.Status);
        original.Employees.Train(hired.EmployeeId!);
        original.Employees.SetShift(hired.EmployeeId!, 0, 480);
        original.AdvanceRealSeconds(17);

        var restoredService = CreateService(service.CaptureSaveData());
        var restored = new BusinessSimulation(
            restoredService,
            original.CaptureSaveData(),
            new StatefulTestRandomSource(1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        Assert.Equivalent(
            original.Employees.GetSnapshot(),
            restored.Employees.GetSnapshot(),
            strict: true);
        Assert.Equivalent(original.CaptureSaveData(), restored.CaptureSaveData(), strict: true);
    }

    [Fact]
    public void CaptureSaveData_RequiresStatefulRandomness()
    {
        var simulation = new BusinessSimulation(
            CreateService(),
            [],
            new ScriptedRandomSource());

        var exception = Assert.Throws<InvalidOperationException>(simulation.CaptureSaveData);

        Assert.Contains("IStatefulRandomSource", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_RejectsEmployeeAssignedToAStoreThatIsNotOpen()
    {
        var service = CreateService();
        var original = new BusinessSimulation(
            service,
            [AssignCashier("zeta-store", "cashier", 1_000)],
            new StatefulTestRandomSource(42));
        var state = original.CaptureSaveData();
        var corrupted = state with
        {
            Employees = state.Employees
                .Select(employee => employee with { StoreId = "missing-store" })
                .ToArray()
        };
        var restoredService = CreateService(service.CaptureSaveData());

        Assert.Throws<ArgumentException>(() =>
            new BusinessSimulation(
                restoredService,
                corrupted,
                new StatefulTestRandomSource(1)));
    }

    [Fact]
    public void CaptureAndRestore_PreservesStaffAssignedToAStoreOpenedLater()
    {
        var service = CreateService();
        var original = new BusinessSimulation(
            service,
            [AssignCashier("alpha-store", "future-cashier", 1_000)],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        var businessSave = service.CaptureSaveData();
        var simulationSave = original.CaptureSaveData();
        var restoredService = CreateService(businessSave);
        var restored = new BusinessSimulation(
            restoredService,
            simulationSave,
            new StatefulTestRandomSource(1),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        service.OpenStore("alpha-store");
        restoredService.OpenStore("alpha-store");
        original.AdvanceRealSecond();
        restored.AdvanceRealSecond();

        var originalStore = original.GetSnapshot().Stores.Single(store => store.StoreId == "alpha-store");
        var restoredStore = restored.GetSnapshot().Stores.Single(store => store.StoreId == "alpha-store");
        Assert.Equal(959, originalStore.ServicePermille);
        Assert.Equivalent(originalStore, restoredStore, strict: true);
    }

    [Fact]
    public void OptionsAndAssignments_RejectInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(baseArrivalBasisPoints: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(basePurchaseBasisPoints: 10_001));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(baseCheckoutMinutes: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(visitorDirtPermille: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(cleanerBaseRecoveryPermille: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BusinessSimulationOptions(initialCleanlinessPermille: 1_001));
        Assert.Throws<ArgumentException>(() =>
            new StoreEmployeeAssignment(" ", CreateEmployee("cashier", EmployeeRole.Cashier, 1_000, 60)));
        Assert.Throws<ArgumentNullException>(() => new StoreEmployeeAssignment("store", null!));
    }

    private static BusinessSimulationOptions CreateAlwaysBusyOptions() =>
        new(baseArrivalBasisPoints: 10_000, basePurchaseBasisPoints: 10_000);

    private static StoreEmployeeAssignment AssignCashier(
        string storeId,
        string employeeId,
        int efficiencyPermille,
        long hourlyWageCents = 60) =>
        new(storeId, CreateEmployee(employeeId, EmployeeRole.Cashier, efficiencyPermille, hourlyWageCents));

    private static StoreEmployeeAssignment AssignCleaner(
        string storeId,
        string employeeId,
        int efficiencyPermille) =>
        new(storeId, CreateEmployee(employeeId, EmployeeRole.Cleaner, efficiencyPermille, hourlyWageCents: 60));

    private static StoreEmployeeAssignment AssignRole(
        string storeId,
        string employeeId,
        EmployeeRole role,
        int efficiencyPermille) =>
        new(storeId, CreateEmployee(employeeId, role, efficiencyPermille, hourlyWageCents: 60));

    private static Employee CreateEmployee(
        string id,
        EmployeeRole role,
        int efficiencyPermille,
        long hourlyWageCents) =>
        new(new EmployeeId(id), id, role, efficiencyPermille, new Money(hourlyWageCents));

    private static BusinessGameService CreateService(long openingCashCents = 100_000) =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 100, "ambient")],
            [
                new ShopDefinition(new ShopId("zeta-store"), "街角店", 1, Money.Zero),
                new ShopDefinition(new ShopId("alpha-store"), "车站店", 1, Money.Zero)
            ],
            new LevelCurve([0, 100]),
            starterShopId: "zeta-store",
            openingCashCents,
            experiencePerItemSold: 1);

    private static BusinessGameService CreateService(BusinessSaveData restoredState) =>
        new(
            [new ProductDefinition("water", "矿泉水", 100, 200, 100, "ambient")],
            [
                new ShopDefinition(new ShopId("zeta-store"), "街角店", 1, Money.Zero),
                new ShopDefinition(new ShopId("alpha-store"), "车站店", 1, Money.Zero)
            ],
            new LevelCurve([0, 100]),
            restoredState,
            experiencePerItemSold: 1);
}
