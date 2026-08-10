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
    public void TaskIds_AreStableAndContainOnlyInvestorLoopTasks()
    {
        Assert.Equal(
            [
                OnboardingTaskId.ReviewEconomy,
                OnboardingTaskId.ChooseStoreStrategy,
                OnboardingTaskId.CompleteFirstSale,
                OnboardingTaskId.ReachPositiveDay,
                OnboardingTaskId.MakeFirstInvestment,
                OnboardingTaskId.OpenSecondStore
            ],
            Enum.GetValues<OnboardingTaskId>());
    }

    [Fact]
    public void CreateSnapshot_ForNewSession_StartsAtEconomyReview()
    {
        var snapshot = OnboardingProgressService.CreateSnapshot(Simulation(), Procurement());

        Assert.Equal(6, snapshot.TotalTasks);
        Assert.Equal(0, snapshot.CompletedTasks);
        Assert.Equal(OnboardingTaskId.ReviewEconomy, snapshot.CurrentTaskId);
    }

    [Theory]
    [MemberData(nameof(CompletionCases))]
    public void CreateSnapshot_CompletesTaskFromInvestorLoopEvidence(
        OnboardingTaskId expectedCompleted,
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement)
    {
        var snapshot = OnboardingProgressService.CreateSnapshot(simulation, procurement);

        Assert.Contains(snapshot.Tasks, task => task.Id == expectedCompleted && task.IsCompleted);
    }

    [Fact]
    public void CreateSnapshot_DefaultBalancedAutomationDoesNotPretendPlayerChoseStrategy()
    {
        var procurement = Procurement(
            [new AutoRestockPolicy("store-1", "water", true, 3, 7, "regional-distributor", true)]);

        var snapshot = OnboardingProgressService.CreateSnapshot(Simulation(), procurement);

        Assert.False(snapshot.Tasks.Single(task => task.Id == OnboardingTaskId.ChooseStoreStrategy).IsCompleted);
    }

    [Fact]
    public void CreateSnapshot_WhenAllTasksComplete_HasNoCurrentTask()
    {
        var simulation = Simulation(
            gameMinute: 1_440,
            stores:
            [
                Store(revenueCents: 200, salePriceCents: 230, growth: Growth(expansionLevel: 1)),
                Store(id: "store-2")
            ],
            lastCompletedDay: DayReport(netProfitCents: 50));

        var snapshot = OnboardingProgressService.CreateSnapshot(simulation, Procurement());

        Assert.Equal(6, snapshot.CompletedTasks);
        Assert.Null(snapshot.CurrentTaskId);
        Assert.True(snapshot.IsComplete);
    }

    [Fact]
    public void Snapshot_RejectsInvalidOrOutOfOrderTasks()
    {
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot([], completedTasks: 0, currentTaskId: null));

        var outOfOrder = FullTaskStates();
        (outOfOrder[0], outOfOrder[1]) = (outOfOrder[1], outOfOrder[0]);
        Assert.Throws<ArgumentException>(() =>
            new OnboardingSnapshot(
                outOfOrder,
                completedTasks: 0,
                currentTaskId: OnboardingTaskId.ReviewEconomy));
    }

    [Fact]
    public void Snapshot_DefensivelyCopiesTasks()
    {
        var tasks = FullTaskStates().ToList();
        var snapshot = new OnboardingSnapshot(
            tasks,
            completedTasks: 0,
            currentTaskId: OnboardingTaskId.ReviewEconomy);

        tasks[0] = tasks[0] with { IsCompleted = true };

        Assert.False(snapshot.Tasks[0].IsCompleted);
        Assert.IsNotType<List<OnboardingTaskState>>(snapshot.Tasks);
    }

    public static TheoryData<OnboardingTaskId, BusinessSimulationSnapshot, ProcurementSnapshot> CompletionCases() =>
        new()
        {
            { OnboardingTaskId.ReviewEconomy, Simulation(gameMinute: 1), Procurement() },
            { OnboardingTaskId.ChooseStoreStrategy, Simulation(stores: [Store(salePriceCents: 230)]), Procurement() },
            { OnboardingTaskId.CompleteFirstSale, Simulation(stores: [Store(revenueCents: 200)]), Procurement() },
            { OnboardingTaskId.ReachPositiveDay, Simulation(lastCompletedDay: DayReport(netProfitCents: 1)), Procurement() },
            { OnboardingTaskId.MakeFirstInvestment, Simulation(stores: [Store(growth: Growth(shelfLevel: 1))]), Procurement() },
            { OnboardingTaskId.OpenSecondStore, Simulation(stores: [Store(), Store(id: "store-2")]), Procurement() }
        };

    private static OnboardingTaskState[] FullTaskStates(bool completed = false) =>
    [
        new(OnboardingTaskId.ReviewEconomy, completed),
        new(OnboardingTaskId.ChooseStoreStrategy, completed),
        new(OnboardingTaskId.CompleteFirstSale, completed),
        new(OnboardingTaskId.ReachPositiveDay, completed),
        new(OnboardingTaskId.MakeFirstInvestment, completed),
        new(OnboardingTaskId.OpenSecondStore, completed)
    ];

    private static BusinessSimulationSnapshot Simulation(
        long gameMinute = 0,
        IReadOnlyList<BusinessStoreSnapshot>? stores = null,
        BusinessDayReport? lastCompletedDay = null) =>
        new(
            gameMinute,
            new BusinessSnapshot(1, 0, 100_000, stores ?? [Store()]),
            [],
            new EmployeeOperationsSnapshot(1, 1, [], []),
            new CommercialStreetSnapshot(
                CommercialStreetTier.Corner,
                StreetWeather.Clear,
                10_000,
                0,
                0,
                []),
            lastCompletedDay);

    private static BusinessStoreSnapshot Store(
        string id = "store-1",
        long revenueCents = 0,
        long salePriceCents = 200,
        StoreGrowthSnapshot? growth = null) =>
        new(
            id,
            id,
            revenueCents,
            0,
            0,
            [new ProductSnapshot("water", "Water", 100, salePriceCents, 0, 10, "ambient", ReferenceSalePriceCents: 200)],
            Growth: growth);

    private static StoreGrowthSnapshot Growth(
        int expansionLevel = 0,
        int shelfLevel = 0,
        int decorationLevel = 0) =>
        new(
            "store-1",
            expansionLevel,
            shelfLevel,
            decorationLevel,
            1,
            1,
            1,
            1_000,
            0,
            null,
            null,
            null,
            0,
            0,
            null);

    private static BusinessDayReport DayReport(long netProfitCents) =>
        new(
            1,
            [new StoreDayReport("store-1", 10, 8, 8, 0, 1_600, 800, 200, netProfitCents, 900, 0)]);

    private static ProcurementSnapshot Procurement(
        IReadOnlyList<AutoRestockPolicy>? policies = null) =>
        new([], [], policies ?? []);
}
