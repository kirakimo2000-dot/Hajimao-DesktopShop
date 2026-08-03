using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed class BusinessSimulation
{
    private readonly object _gate = new();
    private readonly BusinessGameService _game;
    private readonly IRandomSource _random;
    private readonly BusinessSimulationOptions _options;
    private readonly SimulationClock _clock;
    private readonly IStatefulRandomSource? _statefulRandom;
    private readonly Dictionary<string, Employee[]> _staffByStore;
    private readonly Dictionary<string, StoreRuntime> _stores = new(StringComparer.Ordinal);
    private BusinessDayReport? _lastCompletedDay;

    public BusinessSimulation(
        BusinessGameService game,
        IEnumerable<StoreEmployeeAssignment> assignments,
        IRandomSource random,
        BusinessSimulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(random);

        _game = game;
        _random = random;
        _statefulRandom = random as IStatefulRandomSource;
        _options = options ?? new BusinessSimulationOptions();
        _clock = new SimulationClock();
        _staffByStore = CreateStaffMap(assignments, nameof(assignments));
        SynchronizeStores();
    }

    public BusinessSimulation(
        BusinessGameService game,
        BusinessSimulationSaveData restoredState,
        IStatefulRandomSource random,
        BusinessSimulationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(restoredState);
        ArgumentNullException.ThrowIfNull(random);
        if (restoredState.GameMinute < 0 || restoredState.RandomState == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restoredState));
        }

        var employeeSaves = restoredState.Employees?.ToArray()
            ?? throw new ArgumentException("Restored employees are required.", nameof(restoredState));
        var assignments = employeeSaves.Select(RestoreAssignment).ToArray();

        _game = game;
        _random = random;
        _statefulRandom = random;
        _options = options ?? new BusinessSimulationOptions();
        _clock = new SimulationClock(restoredState.GameMinute);
        _staffByStore = CreateStaffMap(assignments, nameof(restoredState));
        _statefulRandom.RestoreState(restoredState.RandomState);
        _lastCompletedDay = restoredState.LastCompletedDay;
        RestoreStores(restoredState.Stores);
    }

    public void AdvanceRealSecond() => AdvanceRealSeconds(1);

    public void AdvanceRealSeconds(int seconds)
    {
        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        lock (_gate)
        {
            for (var second = 0; second < seconds; second++)
            {
                _clock.AdvanceRealSecond(ProcessTick);
            }
        }
    }

    public BusinessSimulationSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var business = _game.GetSnapshot();
            var stores = _stores.Values
                .OrderBy(store => store.StoreId, StringComparer.Ordinal)
                .Select(store => CreateStoreSnapshot(
                    store,
                    business.Stores.Single(snapshot => snapshot.Id == store.StoreId)))
                .ToArray();
            return new BusinessSimulationSnapshot(
                _clock.GameMinute,
                business,
                Array.AsReadOnly(stores),
                _lastCompletedDay);
        }
    }

    public BusinessSimulationSaveData CaptureSaveData()
    {
        lock (_gate)
        {
            if (_statefulRandom is null)
            {
                throw new InvalidOperationException(
                    "Complete simulation saves require an IStatefulRandomSource.");
            }

            var employees = _staffByStore
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SelectMany(pair => pair.Value
                    .OrderBy(employee => employee.Id.Value, StringComparer.Ordinal)
                    .Select(employee =>
                    {
                        var work = employee.CaptureWorkState();
                        return new EmployeeAssignmentSaveData(
                            pair.Key,
                            employee.Id.Value,
                            employee.Name,
                            employee.Role,
                            employee.EfficiencyPermille,
                            employee.HourlyWage.Cents,
                            work.WorkedMinutes,
                            work.TotalWagesAccrued.Cents,
                            work.WageRemainderCents);
                    }))
                .ToArray();
            var stores = _stores.Values
                .OrderBy(store => store.StoreId, StringComparer.Ordinal)
                .Select(store => store.CaptureSaveData())
                .ToArray();
            return new BusinessSimulationSaveData(
                _clock.GameMinute,
                _statefulRandom.State,
                Array.AsReadOnly(employees),
                Array.AsReadOnly(stores),
                _lastCompletedDay);
        }
    }

    private static Dictionary<string, Employee[]> CreateStaffMap(
        IEnumerable<StoreEmployeeAssignment> assignments,
        string parameterName)
    {
        var assignmentArray = assignments.ToArray();
        if (assignmentArray.Any(assignment => assignment is null))
        {
            throw new ArgumentException("Employee assignments cannot contain null.", parameterName);
        }

        var duplicateEmployee = assignmentArray
            .GroupBy(assignment => assignment.Employee.Id.Value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEmployee is not null)
        {
            throw new ArgumentException(
                $"Employee '{duplicateEmployee.Key}' cannot be assigned more than once.",
                parameterName);
        }

        return assignmentArray
            .GroupBy(assignment => assignment.StoreId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(assignment => assignment.Employee)
                    .OrderBy(employee => employee.Id.Value, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static StoreEmployeeAssignment RestoreAssignment(EmployeeAssignmentSaveData saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        var employee = Employee.Restore(
            new EmployeeId(saved.EmployeeId),
            saved.Name,
            saved.Role,
            saved.EfficiencyPermille,
            new Money(saved.HourlyWageCents),
            new EmployeeWorkState(
                saved.WorkedMinutes,
                new Money(saved.TotalWagesAccruedCents),
                saved.WageRemainderCents));
        return new StoreEmployeeAssignment(saved.StoreId, employee);
    }

    private void RestoreStores(IReadOnlyList<StoreRuntimeSaveData> savedStores)
    {
        ArgumentNullException.ThrowIfNull(savedStores);
        var saves = savedStores.ToArray();
        if (saves.Any(store => store is null))
        {
            throw new ArgumentException("Restored stores cannot contain null.", nameof(savedStores));
        }

        var duplicate = saves
            .GroupBy(store => store.StoreId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Restored store runtime '{duplicate.Key}' is duplicated.",
                nameof(savedStores));
        }

        var business = _game.GetSnapshot();
        var openIds = business.Stores.Select(store => store.Id).Order(StringComparer.Ordinal).ToArray();
        var savedIds = saves.Select(store => store.StoreId).Order(StringComparer.Ordinal).ToArray();
        if (!openIds.SequenceEqual(savedIds, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Restored runtime stores must exactly match the open business stores.",
                nameof(savedStores));
        }

        var unknownStaffStore = _staffByStore.Keys
            .Where(storeId => !_game.ContainsStoreDefinition(storeId))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
        if (unknownStaffStore is not null)
        {
            throw new ArgumentException(
                $"Restored employee assignment references unopened store '{unknownStaffStore}'.",
                nameof(savedStores));
        }

        foreach (var saved in saves)
        {
            var store = business.Stores.Single(snapshot => snapshot.Id == saved.StoreId);
            _staffByStore.TryGetValue(saved.StoreId, out var staff);
            _stores.Add(saved.StoreId, new StoreRuntime(store, staff ?? [], saved));
        }
    }

    private void ProcessTick()
    {
        SynchronizeStores();
        foreach (var store in _stores.Values.OrderBy(runtime => runtime.StoreId, StringComparer.Ordinal))
        {
            ProcessStore(store);
            store.RecordQueueSample();
        }

        var completedMinute = checked(_clock.GameMinute + 1L);
        if (completedMinute % 1_440L == 0)
        {
            CompleteDay(checked((int)(completedMinute / 1_440L)));
        }
    }

    private void SynchronizeStores()
    {
        foreach (var store in _game.GetSnapshot().Stores.OrderBy(store => store.Id, StringComparer.Ordinal))
        {
            if (_stores.ContainsKey(store.Id))
            {
                continue;
            }

            _staffByStore.TryGetValue(store.Id, out var staff);
            _stores.Add(
                store.Id,
                new StoreRuntime(store, staff ?? [], _options.InitialCleanlinessPermille));
        }
    }

    private void CompleteDay(int dayNumber)
    {
        var business = _game.GetSnapshot();
        var reports = _stores.Values
            .OrderBy(runtime => runtime.StoreId, StringComparer.Ordinal)
            .Select(runtime =>
            {
                var store = business.Stores.Single(snapshot => snapshot.Id == runtime.StoreId);
                var report = runtime.CreateDayReport(store);
                runtime.StartNextDay(store);
                return report;
            })
            .ToArray();
        _lastCompletedDay = new BusinessDayReport(dayNumber, Array.AsReadOnly(reports));
    }

    private void ProcessStore(StoreRuntime runtime)
    {
        var paidEmployees = PayEmployees(runtime);
        ProcessCleaners(runtime, paidEmployees);
        runtime.ServicePermille = CalculateServicePermille(runtime, paidEmployees);

        var cashier = runtime.Employees.FirstOrDefault(employee =>
            employee.Role == EmployeeRole.Cashier && paidEmployees.Contains(employee.Id));
        ProcessCheckout(runtime, cashier);
        TryVisitAndQueuePurchase(runtime);
    }

    private HashSet<EmployeeId> PayEmployees(StoreRuntime runtime)
    {
        var paid = new HashSet<EmployeeId>();
        foreach (var employee in runtime.Employees)
        {
            var payment = _game.PayEmployeeMinute(runtime.StoreId, employee);
            if (payment.Status == WagePaymentStatus.Success)
            {
                paid.Add(employee.Id);
            }
            else
            {
                runtime.WagePaymentFailures++;
            }
        }

        return paid;
    }

    private void ProcessCleaners(StoreRuntime runtime, HashSet<EmployeeId> paidEmployees)
    {
        foreach (var cleaner in runtime.Employees.Where(employee =>
                     employee.Role == EmployeeRole.Cleaner && paidEmployees.Contains(employee.Id)))
        {
            var scaledRecovery = checked(
                (long)_options.CleanerBaseRecoveryPermille * cleaner.EfficiencyPermille / 1_000L);
            var recovery = checked((int)Math.Clamp(scaledRecovery, 1L, 1_000L));
            runtime.CleanlinessPermille = Math.Min(1_000, runtime.CleanlinessPermille + recovery);
        }
    }

    private static int CalculateServicePermille(
        StoreRuntime runtime,
        HashSet<EmployeeId> paidEmployees)
    {
        var customerFacing = runtime.Employees
            .Where(employee => paidEmployees.Contains(employee.Id)
                && employee.Role is EmployeeRole.Cashier
                    or EmployeeRole.SalesAssistant
                    or EmployeeRole.Manager)
            .ToArray();
        if (customerFacing.Length == 0)
        {
            return 0;
        }

        var average = customerFacing.Sum(employee => (long)employee.EfficiencyPermille)
            / customerFacing.Length;
        return checked((int)Math.Clamp(average, 0L, 2_000L));
    }

    private void ProcessCheckout(StoreRuntime runtime, Employee? cashier)
    {
        if (runtime.ActiveCheckout is not null && cashier is not null)
        {
            runtime.ActiveCheckout.RemainingMinutes--;
            if (runtime.ActiveCheckout.RemainingMinutes == 0)
            {
                var result = _game.Sell(runtime.StoreId, runtime.ActiveCheckout.ProductId, 1);
                if (result.Sale.Status == SaleStatus.Success)
                {
                    runtime.CompletedSales++;
                }
                else
                {
                    runtime.LostSales++;
                }

                runtime.ActiveCheckout = null;
            }
        }

        if (runtime.ActiveCheckout is null
            && cashier is not null
            && runtime.CheckoutQueue.TryDequeue(out var productId))
        {
            runtime.ActiveCheckout = new ActiveCheckout(
                productId,
                cashier.CalculateTaskMinutes(_options.BaseCheckoutMinutes));
        }
    }

    private void TryVisitAndQueuePurchase(StoreRuntime runtime)
    {
        var store = _game.GetSnapshot().Stores.Single(snapshot => snapshot.Id == runtime.StoreId);
        var arrival = CalculateArrivalDemand(runtime, store);
        if (_random.NextDouble() >= arrival.FinalBasisPoints / 10_000d)
        {
            return;
        }

        runtime.Visitors++;
        runtime.CleanlinessPermille = Math.Max(
            0,
            runtime.CleanlinessPermille - _options.VisitorDirtPermille);

        var available = store.Products.Where(product => product.Quantity > 0).ToArray();
        if (available.Length == 0)
        {
            runtime.LostSales++;
            return;
        }

        var selected = available[_random.Next(available.Length)];
        var purchase = DemandModel.CalculatePurchase(new DemandContext(
            _options.BasePurchaseBasisPoints,
            CalculatePriceIndex(selected),
            runtime.ServicePermille,
            runtime.QueueLength,
            runtime.CleanlinessPermille,
            CurrentMinuteOfDay));
        if (_random.NextDouble() >= purchase.FinalBasisPoints / 10_000d)
        {
            runtime.LostSales++;
            return;
        }

        runtime.AcceptedPurchases++;
        runtime.CheckoutQueue.Enqueue(selected.Id);
    }

    private StoreOperationsSnapshot CreateStoreSnapshot(
        StoreRuntime runtime,
        BusinessStoreSnapshot store) =>
        new(
            runtime.StoreId,
            runtime.Visitors,
            runtime.AcceptedPurchases,
            runtime.CompletedSales,
            runtime.LostSales,
            runtime.QueueLength,
            runtime.CleanlinessPermille,
            runtime.ServicePermille,
            runtime.WagePaymentFailures,
            CalculateArrivalDemand(runtime, store));

    private DemandBreakdown CalculateArrivalDemand(
        StoreRuntime runtime,
        BusinessStoreSnapshot store) =>
        DemandModel.CalculateArrival(new DemandContext(
            _options.BaseArrivalBasisPoints,
            CalculateAveragePriceIndex(store.Products),
            runtime.ServicePermille,
            runtime.QueueLength,
            runtime.CleanlinessPermille,
            CurrentMinuteOfDay));

    private int CurrentMinuteOfDay => checked((int)(_clock.GameMinute % 1_440L));

    private static int CalculateAveragePriceIndex(IReadOnlyList<ProductSnapshot> products)
    {
        var available = products.Where(product => product.Quantity > 0).ToArray();
        if (available.Length == 0)
        {
            return 10_000;
        }

        var total = available.Sum(product => (long)CalculatePriceIndex(product));
        return checked((int)(total / available.Length));
    }

    private static int CalculatePriceIndex(ProductSnapshot product)
    {
        var referencePrice = product.ReferenceSalePriceCents > 0
            ? product.ReferenceSalePriceCents
            : product.SalePriceCents;
        var index = checked(product.SalePriceCents * 10_000L / referencePrice);
        return checked((int)Math.Clamp(index, 1L, 30_000L));
    }

    private sealed class StoreRuntime
    {
        public StoreRuntime(
            BusinessStoreSnapshot store,
            Employee[] employees,
            int cleanlinessPermille)
        {
            StoreId = store.Id;
            Employees = employees;
            CleanlinessPermille = cleanlinessPermille;
            StartNextDay(store);
        }

        public StoreRuntime(
            BusinessStoreSnapshot store,
            Employee[] employees,
            StoreRuntimeSaveData saved)
        {
            ArgumentNullException.ThrowIfNull(saved);
            if (!string.Equals(store.Id, saved.StoreId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Restored runtime store ID does not match.", nameof(saved));
            }

            if (saved.Visitors < 0
                || saved.AcceptedPurchases < 0
                || saved.CompletedSales < 0
                || saved.LostSales < 0
                || saved.WagePaymentFailures < 0
                || saved.DayTickCount < 0
                || saved.DayQueueLengthTotal < 0
                || saved.CleanlinessPermille is < 0 or > 1_000
                || saved.ServicePermille is < 0 or > 2_000)
            {
                throw new ArgumentOutOfRangeException(nameof(saved));
            }

            if (saved.DayStartVisitors is < 0
                || saved.DayStartAcceptedPurchases is < 0
                || saved.DayStartCompletedSales is < 0
                || saved.DayStartLostSales is < 0
                || saved.DayStartVisitors > saved.Visitors
                || saved.DayStartAcceptedPurchases > saved.AcceptedPurchases
                || saved.DayStartCompletedSales > saved.CompletedSales
                || saved.DayStartLostSales > saved.LostSales)
            {
                throw new ArgumentException("Restored day baselines are inconsistent.", nameof(saved));
            }

            var knownProducts = store.Products
                .Select(product => product.Id)
                .ToHashSet(StringComparer.Ordinal);
            var queued = saved.CheckoutQueue?.ToArray()
                ?? throw new ArgumentException("Restored checkout queue is required.", nameof(saved));
            if (queued.Any(productId => !knownProducts.Contains(productId)))
            {
                throw new ArgumentException("Restored checkout queue contains an unknown product.", nameof(saved));
            }

            if (saved.ActiveCheckout is { } active
                && (active.RemainingMinutes <= 0 || !knownProducts.Contains(active.ProductId)))
            {
                throw new ArgumentException("Restored active checkout is invalid.", nameof(saved));
            }

            StoreId = store.Id;
            Employees = employees;
            foreach (var productId in queued)
            {
                CheckoutQueue.Enqueue(productId);
            }

            ActiveCheckout = saved.ActiveCheckout is null
                ? null
                : new ActiveCheckout(
                    saved.ActiveCheckout.ProductId,
                    saved.ActiveCheckout.RemainingMinutes);
            Visitors = saved.Visitors;
            AcceptedPurchases = saved.AcceptedPurchases;
            CompletedSales = saved.CompletedSales;
            LostSales = saved.LostSales;
            CleanlinessPermille = saved.CleanlinessPermille;
            ServicePermille = saved.ServicePermille;
            WagePaymentFailures = saved.WagePaymentFailures;
            DayStartVisitors = saved.DayStartVisitors;
            DayStartAcceptedPurchases = saved.DayStartAcceptedPurchases;
            DayStartCompletedSales = saved.DayStartCompletedSales;
            DayStartLostSales = saved.DayStartLostSales;
            DayStartRevenueCents = saved.DayStartRevenueCents;
            DayStartGrossProfitCents = saved.DayStartGrossProfitCents;
            DayStartWageCostCents = saved.DayStartWageCostCents;
            DayQueueLengthTotal = saved.DayQueueLengthTotal;
            DayTickCount = saved.DayTickCount;
        }

        public string StoreId { get; }

        public Employee[] Employees { get; }

        public Queue<string> CheckoutQueue { get; } = [];

        public ActiveCheckout? ActiveCheckout { get; set; }

        public int Visitors { get; set; }

        public int AcceptedPurchases { get; set; }

        public int CompletedSales { get; set; }

        public int LostSales { get; set; }

        public int CleanlinessPermille { get; set; }

        public int ServicePermille { get; set; }

        public int WagePaymentFailures { get; set; }

        public int QueueLength => CheckoutQueue.Count + (ActiveCheckout is null ? 0 : 1);

        public StoreRuntimeSaveData CaptureSaveData() =>
            new(
                StoreId,
                Visitors,
                AcceptedPurchases,
                CompletedSales,
                LostSales,
                Array.AsReadOnly(CheckoutQueue.ToArray()),
                ActiveCheckout is null
                    ? null
                    : new ActiveCheckoutSaveData(
                        ActiveCheckout.ProductId,
                        ActiveCheckout.RemainingMinutes),
                CleanlinessPermille,
                ServicePermille,
                WagePaymentFailures,
                DayStartVisitors,
                DayStartAcceptedPurchases,
                DayStartCompletedSales,
                DayStartLostSales,
                DayStartRevenueCents,
                DayStartGrossProfitCents,
                DayStartWageCostCents,
                DayQueueLengthTotal,
                DayTickCount);

        private int DayStartVisitors { get; set; }

        private int DayStartAcceptedPurchases { get; set; }

        private int DayStartCompletedSales { get; set; }

        private int DayStartLostSales { get; set; }

        private long DayStartRevenueCents { get; set; }

        private long DayStartGrossProfitCents { get; set; }

        private long DayStartWageCostCents { get; set; }

        private long DayQueueLengthTotal { get; set; }

        private int DayTickCount { get; set; }

        public void RecordQueueSample()
        {
            DayQueueLengthTotal = checked(DayQueueLengthTotal + QueueLength);
            DayTickCount = checked(DayTickCount + 1);
        }

        public StoreDayReport CreateDayReport(BusinessStoreSnapshot store)
        {
            var revenue = checked(store.RevenueCents - DayStartRevenueCents);
            var grossProfit = checked(store.GrossProfitCents - DayStartGrossProfitCents);
            var wageCost = checked(store.WageCostCents - DayStartWageCostCents);
            var averageQueue = DayTickCount == 0
                ? 0
                : checked((int)(DayQueueLengthTotal * 10_000L / DayTickCount));
            return new StoreDayReport(
                StoreId,
                checked(Visitors - DayStartVisitors),
                checked(AcceptedPurchases - DayStartAcceptedPurchases),
                checked(CompletedSales - DayStartCompletedSales),
                checked(LostSales - DayStartLostSales),
                revenue,
                grossProfit,
                wageCost,
                checked(grossProfit - wageCost),
                CleanlinessPermille,
                averageQueue);
        }

        public void StartNextDay(BusinessStoreSnapshot store)
        {
            DayStartVisitors = Visitors;
            DayStartAcceptedPurchases = AcceptedPurchases;
            DayStartCompletedSales = CompletedSales;
            DayStartLostSales = LostSales;
            DayStartRevenueCents = store.RevenueCents;
            DayStartGrossProfitCents = store.GrossProfitCents;
            DayStartWageCostCents = store.WageCostCents;
            DayQueueLengthTotal = 0;
            DayTickCount = 0;
        }
    }

    private sealed class ActiveCheckout(string productId, int remainingMinutes)
    {
        public string ProductId { get; } = productId;

        public int RemainingMinutes { get; set; } = remainingMinutes;
    }
}
