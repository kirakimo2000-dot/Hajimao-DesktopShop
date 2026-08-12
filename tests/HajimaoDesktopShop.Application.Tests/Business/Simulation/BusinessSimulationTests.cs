using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Events;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Business;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

[Collection(SimulationPerformanceCollection.Name)]
public sealed class BusinessSimulationTests
{
    [Fact]
    public void Advance_TenDaysAcrossEightStores_StaysWithinSimulationBudget()
    {
        var service = CreateStreetService(storeCount: 8);
        for (var index = 2; index <= 8; index++)
        {
            Assert.Equal(OpenShopStatus.Success, service.OpenStore($"store-{index:D2}").Status);
        }

        var simulation = new BusinessSimulation(
            service,
            [],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(
                baseArrivalBasisPoints: 10_000,
                basePurchaseBasisPoints: 10_000));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        simulation.AdvanceRealSeconds(14_400);

        stopwatch.Stop();
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Eight-store ten-day simulation took {stopwatch.Elapsed}.");
        Assert.Equal(14_400, simulation.GetSnapshot().GameMinute);
    }

    [Fact]
    public void Advance_ProcessesEveryOpenStoreThenRoutesConfiguredStreetOpportunities()
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
        Assert.All(snapshot.Stores, store => Assert.Equal(960, store.ServicePermille));
        Assert.Equal(2, snapshot.Stores.Sum(store => store.Visitors));
        Assert.Equal(2, snapshot.Stores.Sum(store => store.CheckoutQueueLength));
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
    public void StoreFormat_ChangesExplainableArrivalDemandWithoutChangingPlayerControls()
    {
        var discountService = CreateFormattedService("discount");
        var premiumService = CreateFormattedService("premium");
        discountService.PurchaseStock("store-0001", "water", 1);
        premiumService.PurchaseStock("store-0001", "water", 1);
        discountService.ChangePrice("store-0001", "water", 240);
        premiumService.ChangePrice("store-0001", "water", 240);
        var discount = new BusinessSimulation(
            discountService,
            [],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 3_000));
        var premium = new BusinessSimulation(
            premiumService,
            [],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 3_000));

        var discountDemand = Assert.Single(discount.GetSnapshot().Stores).ArrivalDemand;
        var premiumDemand = Assert.Single(premium.GetSnapshot().Stores).ArrivalDemand;
        var discountCapacity = Assert.Single(discountService.GetSnapshot().Stores).Products.Single().Capacity;
        var premiumCapacity = Assert.Single(premiumService.GetSnapshot().Stores).Products.Single().Capacity;

        Assert.Equal(3_660, discountDemand.BaseBasisPoints);
        Assert.Equal(2_340, premiumDemand.BaseBasisPoints);
        Assert.True(discountDemand.PriceAdjustmentBasisPoints < premiumDemand.PriceAdjustmentBasisPoints);
        Assert.Equal(130, discountCapacity);
        Assert.Equal(80, premiumCapacity);
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
    public void MultipleStores_ReceiveSixtyPercentVisitorOpportunitiesPerMinute()
    {
        var service = CreateService();
        Assert.Equal(OpenShopStatus.Success, service.OpenStore("alpha-store").Status);
        service.PurchaseStock("zeta-store", "water", 2);
        service.PurchaseStock("alpha-store", "water", 2);
        var simulation = new BusinessSimulation(
            service,
            [],
            new ScriptedRandomSource(0, 0, 0, 0),
            CreateAlwaysBusyOptions());

        simulation.AdvanceRealSecond();

        var stores = simulation.GetSnapshot().Stores;
        Assert.Equal(2, stores.Sum(store => store.Visitors));
        Assert.Equal(2, simulation.GetSnapshot().Street.Stores.Count);
        Assert.Equal(2, simulation.GetSnapshot().Street.VisitorOpportunities);
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
        Assert.Equal(960, store.ServicePermille);
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
    public void EmployeeWithFinalEnergyMinute_PerformsPaidDutyBeforeBecomingExhausted()
    {
        var employee = Employee.Restore(
            new EmployeeId("cashier"),
            "cashier",
            EmployeeRole.Cashier,
            1_000,
            new Money(60),
            new EmployeeWorkState(0, Money.Zero, 0),
            new EmployeeConditionState(0, 2, 700, 0, 0));
        var simulation = new BusinessSimulation(
            CreateService(),
            [new StoreEmployeeAssignment("zeta-store", employee)],
            new ScriptedRandomSource(),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));

        simulation.AdvanceRealSecond();

        var snapshot = simulation.GetSnapshot();
        Assert.Equal(1, employee.WorkedMinutes);
        Assert.Equal(0, Assert.Single(snapshot.Employees.Employees).EnergyPermille);
        Assert.True(Assert.Single(snapshot.Stores).ServicePermille > 0);
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
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "zeta-store",
            "water",
            IsEnabled: false,
            ReorderPoint: 30,
            TargetQuantity: 75,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: true));
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
    public void MarketEvents_AdvanceOnlyWithRunningSimulationAndRoundTripInSave()
    {
        var definitions = new[]
        {
            new MarketEventDefinition(
                "street-buzz",
                MarketEventScope.Street,
                [],
                240,
                480,
                "街区热议",
                "客流上升。",
                [new MarketEventEffect(MarketEventEffectKind.Traffic, 200)],
                [])
        };
        var service = CreateService();
        var original = new BusinessSimulation(
            service,
            [],
            new StatefulTestRandomSource(42),
            marketEvents: definitions);

        Assert.Empty(Assert.IsType<MarketEventSchedulerSnapshot>(original.GetSnapshot().MarketEvents).ActiveEvents);
        original.AdvanceRealSeconds(240);
        Assert.Single(Assert.IsType<MarketEventSchedulerSnapshot>(original.GetSnapshot().MarketEvents).ActiveEvents);

        var restored = new BusinessSimulation(
            CreateService(service.CaptureSaveData()),
            original.CaptureSaveData(),
            new StatefulTestRandomSource(1),
            marketEvents: definitions);

        Assert.Equivalent(
            Assert.IsType<MarketEventSchedulerSnapshot>(original.GetSnapshot().MarketEvents),
            Assert.IsType<MarketEventSchedulerSnapshot>(restored.GetSnapshot().MarketEvents),
            strict: true);
        Assert.Equivalent(
            Assert.IsType<MarketEventSchedulerSnapshot>(original.CaptureSaveData().MarketEvents),
            Assert.IsType<MarketEventSchedulerSnapshot>(restored.CaptureSaveData().MarketEvents),
            strict: true);
    }

    [Fact]
    public void ActiveTrafficEvent_IncreasesArrivalDemandThroughExistingDemandModel()
    {
        var definitions = new[]
        {
            new MarketEventDefinition(
                "street-buzz",
                MarketEventScope.Street,
                [],
                240,
                480,
                "街区热议",
                "客流上升。",
                [new MarketEventEffect(MarketEventEffectKind.Traffic, 200)],
                [])
        };
        var baseline = new BusinessSimulation(
            CreateService(),
            [],
            new StatefulTestRandomSource(42));
        var eventful = new BusinessSimulation(
            CreateService(),
            [],
            new StatefulTestRandomSource(42),
            marketEvents: definitions);

        baseline.AdvanceRealSeconds(240);
        eventful.AdvanceRealSeconds(240);

        Assert.True(
            Assert.Single(eventful.GetSnapshot().Stores).ArrivalDemand.FinalBasisPoints
            > Assert.Single(baseline.GetSnapshot().Stores).ArrivalDemand.FinalBasisPoints);
    }

    [Fact]
    public void ActiveProcurementEvent_ChangesAutomaticRestockCost()
    {
        var definitions = new[]
        {
            new MarketEventDefinition(
                "supplier-discount",
                MarketEventScope.Global,
                [],
                240,
                480,
                "供应商让利",
                "本批采购成本下降。",
                [new MarketEventEffect(MarketEventEffectKind.ProcurementCost, -200)],
                [])
        };
        var baselineGame = CreateService();
        var eventGame = CreateService();
        ConfigureDisabledRestock(baselineGame);
        ConfigureDisabledRestock(eventGame);
        var baseline = new BusinessSimulation(
            baselineGame,
            [],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        var eventful = new BusinessSimulation(
            eventGame,
            [],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0),
            marketEvents: definitions);
        baseline.AdvanceRealSeconds(240);
        eventful.AdvanceRealSeconds(240);
        ConfigureEnabledRestock(baselineGame);
        ConfigureEnabledRestock(eventGame);

        baseline.AdvanceRealSecond();
        eventful.AdvanceRealSecond();

        Assert.True(eventGame.GetSnapshot().CashCents > baselineGame.GetSnapshot().CashCents);
        Assert.True(
            Assert.Single(eventGame.GetProcurementSnapshot().PendingOrders).UnitCostCents
            < Assert.Single(baselineGame.GetProcurementSnapshot().PendingOrders).UnitCostCents);
    }

    [Fact]
    public void ActiveEmployeeEfficiencyEvent_ChangesServiceOutput()
    {
        var definitions = new[]
        {
            new MarketEventDefinition(
                "team-rhythm",
                MarketEventScope.Employee,
                [],
                240,
                480,
                "团队进入状态",
                "当班员工效率提高。",
                [new MarketEventEffect(MarketEventEffectKind.EmployeeEfficiency, 200)],
                [])
        };
        var baseline = new BusinessSimulation(
            CreateService(),
            [AssignRole("zeta-store", "assistant", EmployeeRole.SalesAssistant, 1_000)],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0));
        var eventful = new BusinessSimulation(
            CreateService(),
            [AssignRole("zeta-store", "assistant", EmployeeRole.SalesAssistant, 1_000)],
            new StatefulTestRandomSource(42),
            new BusinessSimulationOptions(baseArrivalBasisPoints: 0),
            marketEvents: definitions);

        baseline.AdvanceRealSeconds(241);
        eventful.AdvanceRealSeconds(241);

        Assert.True(
            Assert.Single(eventful.GetSnapshot().Stores).ServicePermille
            > Assert.Single(baseline.GetSnapshot().Stores).ServicePermille);
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
    public void CompletedDay_RefreshesCandidatePoolOnceWithoutPlayerMaintenance()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 1_000_000);
        var initialIds = session.Simulation.GetSnapshot().Employees.Candidates
            .Select(candidate => candidate.CandidateId)
            .ToArray();

        session.Simulation.AdvanceRealSeconds(1_439);
        var beforeBoundaryIds = session.Simulation.GetSnapshot().Employees.Candidates
            .Select(candidate => candidate.CandidateId)
            .ToArray();
        session.Simulation.AdvanceRealSecond();
        var afterBoundaryIds = session.Simulation.GetSnapshot().Employees.Candidates
            .Select(candidate => candidate.CandidateId)
            .ToArray();

        Assert.Equal(initialIds, beforeBoundaryIds);
        Assert.NotEqual(initialIds, afterBoundaryIds);
        Assert.Equal(3, afterBoundaryIds.Length);
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
        Assert.Equal(960, originalStore.ServicePermille);
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

    private static void ConfigureDisabledRestock(BusinessGameService service) =>
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "zeta-store",
            "water",
            IsEnabled: false,
            ReorderPoint: 1,
            TargetQuantity: 6,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: false));

    private static void ConfigureEnabledRestock(BusinessGameService service) =>
        service.ConfigureAutoRestock(new AutoRestockPolicy(
            "zeta-store",
            "water",
            IsEnabled: true,
            ReorderPoint: 1,
            TargetQuantity: 6,
            PreferredChannelId: "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: false));

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

    private static BusinessGameService CreateStreetService(int storeCount)
    {
        var stores = Enumerable.Range(1, storeCount)
            .Select(index => new ShopDefinition(
                new ShopId($"store-{index:D2}"),
                $"店铺 {index}",
                requiredPlayerLevel: 1,
                Money.Zero))
            .ToArray();
        return new BusinessGameService(
            Enumerable.Range(1, 120)
                .Select(index => new ProductDefinition(
                    $"product-{index:D3}",
                    $"商品 {index}",
                    100 + index,
                    240 + index,
                    100,
                    index % 3 == 0 ? "frozen" : index % 2 == 0 ? "chilled" : "ambient",
                    requiredPlayerLevel: 1,
                    categoryId: $"category-{index % 12:D2}"))
                .ToArray(),
            stores,
            new LevelCurve([0]),
            starterShopId: "store-01",
            openingCashCents: 100_000,
            experiencePerItemSold: 1);
    }

    private static BusinessGameService CreateFormattedService(string formatId)
    {
        var formats = new[]
        {
            new StoreFormatDefinition(
                "discount", "平价量贩", 70_000, 70_000,
                1_220, 1_450, 800, 1_250, 800, 1_300,
                "all-day-volume",
                new Dictionary<string, int>
                {
                    ["ambient"] = 1_250,
                    ["chilled"] = 1_000,
                    ["frozen"] = 850
                },
                StorePricingPreset.HighTurnover,
                StoreStockingPreset.FullShelves),
            new StoreFormatDefinition(
                "premium", "精品食品", 90_000, 55_000,
                780, 600, 1_500, 900, 1_500, 800,
                "afternoon-select",
                new Dictionary<string, int>
                {
                    ["ambient"] = 750,
                    ["chilled"] = 1_250,
                    ["frozen"] = 1_400
                },
                StorePricingPreset.HighMargin,
                StoreStockingPreset.Lean)
        };
        var brand = new StoreBrandDefinition(
            $"brand-{formatId}", formatId, "global", formatId,
            "facade", "real-world-name", "review-required");
        return new BusinessGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 100, "ambient")],
            [new ShopDefinition(
                new ShopId("store-0001"),
                new StoreBrandId(brand.Id),
                new StoreFormatId(formatId),
                brand.DisplayName,
                1,
                Money.Zero)],
            new LevelCurve([0, 100]),
            starterShopId: "store-0001",
            openingCashCents: 100_000,
            experiencePerItemSold: 1,
            storeContent: new StoreContentCatalog(formats, [brand]));
    }

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
