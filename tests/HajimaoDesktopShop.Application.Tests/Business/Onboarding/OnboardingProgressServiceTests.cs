using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Onboarding;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Business.Onboarding;

public sealed class OnboardingProgressServiceTests
{
    [Fact]
    public void TaskIds_AreStableAndOrdered()
    {
        Assert.Equal(
            [
                OnboardingTaskId.RestockProduct,
                OnboardingTaskId.AdjustPrice,
                OnboardingTaskId.EnableAutoRestock,
                OnboardingTaskId.CompleteFirstSale,
                OnboardingTaskId.TrainEmployee,
                OnboardingTaskId.UpgradeStore,
                OnboardingTaskId.OpenSecondStore
            ],
            Enum.GetValues<OnboardingTaskId>());
    }

    [Fact]
    public void TaskState_StoresTaskIdentityAndCompletion()
    {
        var state = new OnboardingTaskState(OnboardingTaskId.AdjustPrice, IsCompleted: true);

        Assert.Equal(OnboardingTaskId.AdjustPrice, state.Id);
        Assert.True(state.IsCompleted);
    }

    [Fact]
    public void CreateSnapshot_ForNewSession_StartsAtRestockProduct()
    {
        var snapshot = OnboardingProgressService.CreateSnapshot(Simulation(), Procurement());

        Assert.Equal(7, snapshot.TotalTasks);
        Assert.Equal(0, snapshot.CompletedTasks);
        Assert.Equal(OnboardingTaskId.RestockProduct, snapshot.CurrentTaskId);
        Assert.False(snapshot.IsComplete);
        Assert.All(snapshot.Tasks, task => Assert.False(task.IsCompleted));
    }

    [Theory]
    [MemberData(nameof(CompletionCases))]
    public void CreateSnapshot_CompletesTaskWhenPredicateFactExists(
        OnboardingTaskId expectedCompleted,
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement)
    {
        var snapshot = OnboardingProgressService.CreateSnapshot(simulation, procurement);

        AssertCompleted(snapshot, expectedCompleted);
    }

    [Fact]
    public void CreateSnapshot_CountsOutOfOrderCompletionsButKeepsCurrentAtFirstIncomplete()
    {
        var simulation = Simulation(
            stores:
            [
                Store(products: [Product(salePriceCents: 225, referenceSalePriceCents: 200)]),
                Store(id: "station-store")
            ],
            employees: Employees(trainingLevel: 1));

        var snapshot = OnboardingProgressService.CreateSnapshot(simulation, Procurement());

        AssertCompleted(
            snapshot,
            OnboardingTaskId.AdjustPrice,
            OnboardingTaskId.TrainEmployee,
            OnboardingTaskId.OpenSecondStore);
        Assert.Equal(3, snapshot.CompletedTasks);
        Assert.Equal(OnboardingTaskId.RestockProduct, snapshot.CurrentTaskId);
        Assert.False(snapshot.IsComplete);
    }

    [Fact]
    public void CreateSnapshot_WhenAllTasksComplete_HasNoCurrentTask()
    {
        var simulation = Simulation(
            stores:
            [
                Store(
                    revenueCents: 200,
                    stockPurchaseCostCents: 100,
                    products: [Product(salePriceCents: 225, referenceSalePriceCents: 200)],
                    growth: Growth(shelfLevel: 1)),
                Store(id: "station-store")
            ],
            employees: Employees(trainingLevel: 1));
        var procurement = Procurement(
            autoRestockPolicies:
            [
                new AutoRestockPolicy(
                    "corner-store",
                    "water",
                    IsEnabled: true,
                    ReorderPoint: 2,
                    TargetQuantity: 5,
                    PreferredChannelId: "regional-distributor",
                    UseEmergencySupplierWhenOutOfStock: false)
            ]);

        var snapshot = OnboardingProgressService.CreateSnapshot(simulation, procurement);

        Assert.Equal(7, snapshot.CompletedTasks);
        Assert.Null(snapshot.CurrentTaskId);
        Assert.True(snapshot.IsComplete);
        Assert.All(snapshot.Tasks, task => Assert.True(task.IsCompleted));
    }

    [Fact]
    public void Snapshot_RejectsInvalidTasks()
    {
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot([], completedTasks: 0, currentTaskId: null));
        Assert.Throws<ArgumentNullException>(() =>
            new OnboardingSnapshot(null!, completedTasks: 0, currentTaskId: null));
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [new OnboardingTaskState(OnboardingTaskId.AdjustPrice, IsCompleted: false)],
                completedTasks: 0,
                currentTaskId: OnboardingTaskId.AdjustPrice));
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [
                    new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: false),
                    new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: false)
                ],
                completedTasks: 0,
                currentTaskId: OnboardingTaskId.RestockProduct));
    }

    [Fact]
    public void Snapshot_RejectsIncompleteTaskPrefix()
    {
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: true)],
                completedTasks: 1,
                currentTaskId: null));
    }

    [Fact]
    public void Snapshot_RejectsNullTaskEntry()
    {
        var tasks = FullTaskStates();
        tasks[1] = null!;

        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                tasks,
                completedTasks: 0,
                currentTaskId: OnboardingTaskId.RestockProduct));
    }

    [Fact]
    public void Snapshot_RejectsInconsistentCompletionMetadata()
    {
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: true)],
                completedTasks: 0,
                currentTaskId: null));
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: false)],
                completedTasks: 0,
                currentTaskId: null));
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                [new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: false)],
                completedTasks: 1,
                currentTaskId: OnboardingTaskId.RestockProduct));
    }

    [Fact]
    public void Snapshot_DefensivelyCopiesTasks()
    {
        var tasks = FullTaskStates().ToList();
        var snapshot = new OnboardingSnapshot(
            tasks,
            completedTasks: 0,
            currentTaskId: OnboardingTaskId.RestockProduct);

        tasks[0] = new OnboardingTaskState(OnboardingTaskId.RestockProduct, IsCompleted: true);
        tasks.Add(new OnboardingTaskState(OnboardingTaskId.AdjustPrice, IsCompleted: true));

        Assert.Equal(7, snapshot.Tasks.Count);
        Assert.False(snapshot.Tasks[0].IsCompleted);
        Assert.IsNotType<List<OnboardingTaskState>>(snapshot.Tasks);
    }

    [Fact]
    public void CreateSnapshot_RejectsNullSnapshots()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OnboardingProgressService.CreateSnapshot(null!, Procurement()));
        Assert.Throws<ArgumentNullException>(() =>
            OnboardingProgressService.CreateSnapshot(Simulation(), null!));
    }

    public static TheoryData<OnboardingTaskId, BusinessSimulationSnapshot, ProcurementSnapshot> CompletionCases()
    {
        return new TheoryData<OnboardingTaskId, BusinessSimulationSnapshot, ProcurementSnapshot>
        {
            { OnboardingTaskId.RestockProduct, Simulation(stores: [Store(stockPurchaseCostCents: 100)]), Procurement() },
            { OnboardingTaskId.AdjustPrice, Simulation(stores: [Store(products: [Product(salePriceCents: 225, referenceSalePriceCents: 200)])]), Procurement() },
            { OnboardingTaskId.EnableAutoRestock, Simulation(), Procurement(autoRestockPolicies: [new AutoRestockPolicy("corner-store", "water", true, 2, 5, "regional-distributor", false)]) },
            { OnboardingTaskId.CompleteFirstSale, Simulation(stores: [Store(revenueCents: 100)]), Procurement() },
            { OnboardingTaskId.TrainEmployee, Simulation(employees: Employees(trainingLevel: 1)), Procurement() },
            { OnboardingTaskId.UpgradeStore, Simulation(stores: [Store(growth: Growth(expansionLevel: 1))]), Procurement() },
            { OnboardingTaskId.UpgradeStore, Simulation(stores: [Store(growth: Growth(shelfLevel: 1))]), Procurement() },
            { OnboardingTaskId.UpgradeStore, Simulation(stores: [Store(growth: Growth(decorationLevel: 1))]), Procurement() },
            { OnboardingTaskId.OpenSecondStore, Simulation(stores: [Store(), Store(id: "station-store")]), Procurement() }
        };
    }

    private static void AssertCompleted(
        OnboardingSnapshot snapshot,
        params OnboardingTaskId[] expectedCompleted)
    {
        var completed = snapshot.Tasks
            .Where(task => task.IsCompleted)
            .Select(task => task.Id)
            .ToArray();

        Assert.Equal(expectedCompleted.Order(), completed.Order());
    }

    private static OnboardingTaskState[] FullTaskStates() =>
    [
        new(OnboardingTaskId.RestockProduct, IsCompleted: false),
        new(OnboardingTaskId.AdjustPrice, IsCompleted: false),
        new(OnboardingTaskId.EnableAutoRestock, IsCompleted: false),
        new(OnboardingTaskId.CompleteFirstSale, IsCompleted: false),
        new(OnboardingTaskId.TrainEmployee, IsCompleted: false),
        new(OnboardingTaskId.UpgradeStore, IsCompleted: false),
        new(OnboardingTaskId.OpenSecondStore, IsCompleted: false)
    ];

    private static BusinessSimulationSnapshot Simulation(
        IReadOnlyList<BusinessStoreSnapshot>? stores = null,
        EmployeeOperationsSnapshot? employees = null)
    {
        stores ??= [Store()];

        return new BusinessSimulationSnapshot(
            GameMinute: 0,
            Business: new BusinessSnapshot(
                PlayerLevel: 1,
                TotalExperience: 0,
                CashCents: 100_000,
                Stores: stores),
            Stores: [],
            Employees: employees ?? Employees(),
            Street: new CommercialStreetSnapshot(
                CommercialStreetTier.Corner,
                StreetWeather.Clear,
                SharedTrafficBasisPoints: 10_000,
                VisiblePedestrians: 0,
                VisibleVehicles: 0,
                Stores: []));
    }

    private static BusinessStoreSnapshot Store(
        string id = "corner-store",
        long revenueCents = 0,
        long stockPurchaseCostCents = 0,
        IReadOnlyList<ProductSnapshot>? products = null,
        StoreGrowthSnapshot? growth = null)
    {
        return new BusinessStoreSnapshot(
            Id: id,
            Name: id,
            RevenueCents: revenueCents,
            StockPurchaseCostCents: stockPurchaseCostCents,
            GrossProfitCents: 0,
            Products: products ?? [Product()],
            Growth: growth);
    }

    private static ProductSnapshot Product(
        long salePriceCents = 200,
        long referenceSalePriceCents = 200)
    {
        return new ProductSnapshot(
            Id: "water",
            Name: "Water",
            WholesalePriceCents: 100,
            SalePriceCents: salePriceCents,
            Quantity: 0,
            Capacity: 10,
            ShelfKind: "ambient",
            ReferenceSalePriceCents: referenceSalePriceCents);
    }

    private static ProcurementSnapshot Procurement(
        IReadOnlyList<AutoRestockPolicy>? autoRestockPolicies = null)
    {
        return new ProcurementSnapshot(
            Channels: [],
            PendingOrders: [],
            AutoRestockPolicies: autoRestockPolicies ?? []);
    }

    private static EmployeeOperationsSnapshot Employees(int trainingLevel = 0)
    {
        return new EmployeeOperationsSnapshot(
            CandidateRandomState: 0,
            NextCandidateId: 1,
            Candidates: [],
            Employees:
            [
                new EmployeeOperationsEmployeeSnapshot(
                    EmployeeId: "cashier",
                    Name: "Cashier",
                    Role: EmployeeRole.Cashier,
                    BaseEfficiencyPermille: 1_000,
                    EffectiveEfficiencyPermille: 1_000,
                    HourlyWageCents: 1_000,
                    TrainingLevel: trainingLevel,
                    EnergyPermille: 1_000,
                    SatisfactionPermille: 1_000,
                    StoreId: "corner-store",
                    ShiftStartMinute: 0,
                    ShiftEndMinute: 1_440,
                    IsAlwaysOn: true)
            ]);
    }

    private static StoreGrowthSnapshot Growth(
        int expansionLevel = 0,
        int shelfLevel = 0,
        int decorationLevel = 0)
    {
        return new StoreGrowthSnapshot(
            StoreId: "corner-store",
            ExpansionLevel: expansionLevel,
            ShelfLevel: shelfLevel,
            DecorationLevel: decorationLevel,
            FloorAreaUnits: 1,
            ShelfSlotCount: 1,
            QueueComfortCapacity: 1,
            InventoryCapacityPermille: 1_000,
            AttractionBonusBasisPoints: 0,
            NextExpansionUpgradeCostCents: null,
            NextShelfUpgradeCostCents: null,
            NextDecorationUpgradeCostCents: null,
            PromotionArrivalBonusBasisPoints: 0,
            PromotionPurchaseBonusBasisPoints: 0,
            ActivePromotion: null);
    }
}
