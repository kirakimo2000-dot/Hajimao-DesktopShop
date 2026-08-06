using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Street;
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
    private readonly EmployeeOperationsService _employeeOperations;
    private readonly CommercialStreetTrafficService _streetTraffic;
    private readonly Dictionary<string, StoreRuntime> _stores = new(StringComparer.Ordinal);
    private readonly HashSet<EmployeeId> _lastUnpaidEmployees = [];
    private readonly HashSet<EmployeeId> _lastRestingEmployees = [];
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
        _employeeOperations = CreateEmployeeOperations(game, assignments, nameof(assignments));
        _streetTraffic = new CommercialStreetTrafficService(random);
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
        _game = game;
        _random = random;
        _statefulRandom = random;
        _options = options ?? new BusinessSimulationOptions();
        _clock = new SimulationClock(restoredState.GameMinute);
        _employeeOperations = RestoreEmployeeOperations(
            game,
            employeeSaves,
            restoredState.EmployeeOperations,
            nameof(restoredState));
        _streetTraffic = new CommercialStreetTrafficService(random);
        _statefulRandom.RestoreState(restoredState.RandomState);
        _lastCompletedDay = restoredState.LastCompletedDay;
        RestoreStores(restoredState.Stores);
    }

    public EmployeeOperationsService Employees => _employeeOperations;

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
            SynchronizeStores();
            var business = _game.GetSnapshot();
            var stores = _stores.Values
                .OrderBy(store => store.StoreId, StringComparer.Ordinal)
                .Select(store => CreateStoreSnapshot(
                    store,
                    business.Stores.Single(snapshot => snapshot.Id == store.StoreId)))
                .ToArray();
            var street = CreateStreetSnapshot(business, stores);
            var employeeTasks = CreateEmployeeTasks(business);
            return new BusinessSimulationSnapshot(
                _clock.GameMinute,
                business,
                Array.AsReadOnly(stores),
                _employeeOperations.GetSnapshot(employeeTasks),
                street,
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

            var employees = _employeeOperations.GetRuntimeAssignments()
                .OrderBy(assignment => assignment.StoreId, StringComparer.Ordinal)
                .ThenBy(assignment => assignment.Employee.Id.Value, StringComparer.Ordinal)
                .Select(assignment =>
                    {
                        var employee = assignment.Employee;
                        var work = employee.CaptureWorkState();
                        var condition = employee.CaptureConditionState();
                        return new EmployeeAssignmentSaveData(
                            assignment.StoreId,
                            employee.Id.Value,
                            employee.Name,
                            employee.Role,
                            employee.EfficiencyPermille,
                            employee.HourlyWage.Cents,
                            work.WorkedMinutes,
                            work.TotalWagesAccrued.Cents,
                            work.WageRemainderCents,
                            condition.TrainingLevel,
                            condition.EnergyPermille,
                            condition.SatisfactionPermille,
                            condition.WorkMinutesTowardSatisfactionLoss,
                            condition.RestMinutesTowardSatisfactionGain,
                            assignment.Shift.StartMinute,
                            assignment.Shift.EndMinute,
                            assignment.Shift.IsAlwaysOn);
                    })
                .ToArray();
            var stores = _stores.Values
                .OrderBy(store => store.StoreId, StringComparer.Ordinal)
                .Select(store => store.CaptureSaveData())
                .ToArray();
            var employeeOperations = _employeeOperations.GetSnapshot();
            var candidates = employeeOperations.Candidates
                .Select(candidate => new EmployeeCandidateSaveData(
                    candidate.CandidateId,
                    candidate.Name,
                    candidate.Role,
                    candidate.EfficiencyPermille,
                    candidate.HourlyWage.Cents))
                .ToArray();
            return new BusinessSimulationSaveData(
                _clock.GameMinute,
                _statefulRandom.State,
                Array.AsReadOnly(employees),
                Array.AsReadOnly(stores),
                _lastCompletedDay,
                new EmployeeOperationsSaveData(
                    employeeOperations.CandidateRandomState,
                    employeeOperations.NextCandidateId,
                    Array.AsReadOnly(candidates)));
        }
    }

    private static EmployeeOperationsService CreateEmployeeOperations(
        BusinessGameService game,
        IEnumerable<StoreEmployeeAssignment> assignments,
        string parameterName)
    {
        var staffByStore = CreateStaffMap(assignments, parameterName);
        var operations = new EmployeeOperationsService(game);
        foreach (var pair in staffByStore.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (var employee in pair.Value)
            {
                operations.RegisterExistingEmployee(pair.Key, employee);
            }
        }

        return operations;
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

    private static EmployeeOperationsService RestoreEmployeeOperations(
        BusinessGameService game,
        IEnumerable<EmployeeAssignmentSaveData> savedEmployees,
        EmployeeOperationsSaveData? savedOperations,
        string parameterName)
    {
        var saves = savedEmployees.ToArray();
        if (saves.Any(saved => saved is null))
        {
            throw new ArgumentException("Restored employees cannot contain null.", parameterName);
        }

        var duplicate = saves
            .GroupBy(saved => saved.EmployeeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Employee '{duplicate.Key}' cannot be restored more than once.",
                parameterName);
        }

        EmployeeOperationsService operations;
        if (savedOperations is null)
        {
            operations = new EmployeeOperationsService(game);
        }
        else
        {
            var savedCandidates = savedOperations.Candidates?.ToArray()
                ?? throw new ArgumentException("Restored employee candidates are required.", parameterName);
            if (savedCandidates.Any(candidate => candidate is null))
            {
                throw new ArgumentException("Restored employee candidates cannot contain null.", parameterName);
            }

            operations = new EmployeeOperationsService(
                game,
                savedOperations.CandidateRandomState,
                savedOperations.NextCandidateId,
                savedCandidates.Select(candidate => new EmployeeCandidate(
                    candidate.CandidateId,
                    candidate.Name,
                    candidate.Role,
                    candidate.EfficiencyPermille,
                    new Money(candidate.HourlyWageCents))));
        }

        foreach (var saved in saves.OrderBy(saved => saved.EmployeeId, StringComparer.Ordinal))
        {
            var employee = Employee.Restore(
                new EmployeeId(saved.EmployeeId),
                saved.Name,
                saved.Role,
                saved.EfficiencyPermille,
                new Money(saved.HourlyWageCents),
                new EmployeeWorkState(
                    saved.WorkedMinutes,
                    new Money(saved.TotalWagesAccruedCents),
                    saved.WageRemainderCents),
                new EmployeeConditionState(
                    saved.TrainingLevel,
                    saved.EnergyPermille,
                    saved.SatisfactionPermille,
                    saved.WorkMinutesTowardSatisfactionLoss,
                    saved.RestMinutesTowardSatisfactionGain));
            var shift = saved.IsAlwaysOn
                ? EmployeeShift.CreateLegacyAlwaysOn(saved.EmployeeId, saved.StoreId)
                : new EmployeeShift(
                    saved.EmployeeId,
                    saved.StoreId,
                    saved.ShiftStartMinute,
                    saved.ShiftEndMinute);
            operations.RegisterExistingEmployee(saved.StoreId, employee, shift);
        }

        return operations;
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

        var unknownStaffStore = _employeeOperations.GetRuntimeAssignments()
            .Select(assignment => assignment.StoreId)
            .Distinct(StringComparer.Ordinal)
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
            var staff = GetEmployeesForStore(saved.StoreId);
            _stores.Add(saved.StoreId, new StoreRuntime(store, staff, saved));
        }
    }

    private void ProcessTick()
    {
        SynchronizeStores();
        _lastUnpaidEmployees.Clear();
        _lastRestingEmployees.Clear();
        foreach (var store in _stores.Values.OrderBy(runtime => runtime.StoreId, StringComparer.Ordinal))
        {
            ProcessStoreOperations(store);
        }

        var business = _game.GetSnapshot();
        var storeOperations = _stores.Values
            .OrderBy(runtime => runtime.StoreId, StringComparer.Ordinal)
            .Select(runtime => CreateStoreSnapshot(
                runtime,
                business.Stores.Single(store => store.Id == runtime.StoreId)))
            .ToArray();
        var street = CreateStreetSnapshot(business, storeOperations);
        var visitingStoreId = _streetTraffic.TryRouteVisitor(street);
        if (visitingStoreId is not null)
        {
            ProcessVisitorAndQueuePurchase(_stores[visitingStoreId]);
        }

        foreach (var store in _stores.Values)
        {
            store.RecordQueueSample();
        }

        _game.AdvanceProcurementMinute();
        _game.AdvanceStoreGrowthMinute();

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
            var staff = GetEmployeesForStore(store.Id);
            if (_stores.TryGetValue(store.Id, out var runtime))
            {
                runtime.Employees = staff;
                continue;
            }

            _stores.Add(
                store.Id,
                new StoreRuntime(store, staff, _options.InitialCleanlinessPermille));
        }
    }

    private Employee[] GetEmployeesForStore(string storeId) =>
        _employeeOperations.GetRuntimeAssignments()
            .Where(assignment => string.Equals(
                assignment.StoreId,
                storeId,
                StringComparison.Ordinal))
            .Select(assignment => assignment.Employee)
            .ToArray();

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

    private void ProcessStoreOperations(StoreRuntime runtime)
    {
        var paidEmployees = PayEmployees(runtime);
        var store = _game.GetSnapshot().Stores.Single(snapshot => snapshot.Id == runtime.StoreId);
        var employeeTasks = CreateEmployeeTasks(runtime, store);
        ProcessCleaners(runtime, paidEmployees, employeeTasks);
        runtime.ServicePermille = CalculateServicePermille(runtime, paidEmployees, employeeTasks);

        var cashier = runtime.Employees.FirstOrDefault(employee =>
            paidEmployees.Contains(employee.Id)
            && employeeTasks.GetValueOrDefault(employee.Id.Value)?.Kind == EmployeeTaskKind.Checkout);
        ProcessCheckout(runtime, cashier);
    }

    private HashSet<EmployeeId> PayEmployees(StoreRuntime runtime)
    {
        var paid = new HashSet<EmployeeId>();
        foreach (var assignment in _employeeOperations.GetRuntimeAssignments().Where(assignment =>
                     string.Equals(assignment.StoreId, runtime.StoreId, StringComparison.Ordinal)
                     && (!assignment.Shift.ContainsMinute(CurrentMinuteOfDay)
                         || !assignment.Employee.CanWork)))
        {
            _lastRestingEmployees.Add(assignment.Employee.Id);
        }

        var available = _employeeOperations.ResolveAvailableEmployees(
            runtime.StoreId,
            CurrentMinuteOfDay);
        foreach (var employee in available)
        {
            var payment = _game.PayEmployeeMinute(runtime.StoreId, employee);
            if (payment.Status == WagePaymentStatus.Success)
            {
                _employeeOperations.RecordPaidWorkCondition(employee.Id);
                paid.Add(employee.Id);
            }
            else
            {
                runtime.WagePaymentFailures++;
                _lastUnpaidEmployees.Add(employee.Id);
            }
        }

        return paid;
    }

    private void ProcessCleaners(
        StoreRuntime runtime,
        HashSet<EmployeeId> paidEmployees,
        IReadOnlyDictionary<string, EmployeeTaskSnapshot> employeeTasks)
    {
        foreach (var cleaner in runtime.Employees.Where(employee =>
                     paidEmployees.Contains(employee.Id)
                     && employeeTasks.GetValueOrDefault(employee.Id.Value)?.Kind == EmployeeTaskKind.Clean))
        {
            var recovery = CalculateCleanerRecovery(cleaner);
            runtime.CleanlinessPermille = Math.Min(1_000, runtime.CleanlinessPermille + recovery);
        }
    }

    private static int CalculateServicePermille(
        StoreRuntime runtime,
        HashSet<EmployeeId> paidEmployees,
        IReadOnlyDictionary<string, EmployeeTaskSnapshot> employeeTasks)
    {
        var customerFacing = runtime.Employees
            .Where(employee => paidEmployees.Contains(employee.Id)
                && employeeTasks.GetValueOrDefault(employee.Id.Value)?.Kind
                    == EmployeeTaskKind.CustomerService)
            .ToArray();
        if (customerFacing.Length == 0)
        {
            return 0;
        }

        var average = customerFacing.Sum(employee => (long)employee.EffectiveEfficiencyPermille)
            / customerFacing.Length;
        return checked((int)Math.Clamp(average, 0L, 2_000L));
    }

    private IReadOnlyDictionary<string, EmployeeTaskSnapshot> CreateEmployeeTasks(
        BusinessSnapshot business)
    {
        var tasks = new Dictionary<string, EmployeeTaskSnapshot>(StringComparer.Ordinal);
        foreach (var runtime in _stores.Values.OrderBy(store => store.StoreId, StringComparer.Ordinal))
        {
            var store = business.Stores.Single(snapshot => snapshot.Id == runtime.StoreId);
            foreach (var pair in CreateEmployeeTasks(runtime, store))
            {
                tasks.Add(pair.Key, pair.Value);
            }
        }

        return tasks;
    }

    private IReadOnlyDictionary<string, EmployeeTaskSnapshot> CreateEmployeeTasks(
        StoreRuntime runtime,
        BusinessStoreSnapshot store)
    {
        var assignments = _employeeOperations.GetRuntimeAssignments()
            .Where(assignment => string.Equals(
                assignment.StoreId,
                runtime.StoreId,
                StringComparison.Ordinal))
            .ToArray();
        var workers = assignments
            .Select(assignment => new EmployeeTaskWorker(
                assignment.Employee.Id.Value,
                assignment.Employee.Role,
                ResolveTaskAvailability(assignment)))
            .ToArray();
        var planned = EmployeeTaskPlanner.Plan(workers, CreateTaskDemand(runtime, store));
        return AdjustTaskDurations(runtime, planned);
    }

    private EmployeeTaskAvailability ResolveTaskAvailability(EmployeeRuntimeAssignment assignment)
    {
        if (_lastRestingEmployees.Contains(assignment.Employee.Id)
            || !assignment.Shift.ContainsMinute(CurrentMinuteOfDay)
            || !assignment.Employee.CanWork)
        {
            return EmployeeTaskAvailability.Resting;
        }

        return _lastUnpaidEmployees.Contains(assignment.Employee.Id)
            ? EmployeeTaskAvailability.Unpaid
            : EmployeeTaskAvailability.Working;
    }

    private StoreTaskDemand CreateTaskDemand(StoreRuntime runtime, BusinessStoreSnapshot store)
    {
        var checkoutProductId = runtime.ActiveCheckout?.ProductId;
        if (checkoutProductId is null)
        {
            runtime.CheckoutQueue.TryPeek(out checkoutProductId);
        }

        var checkoutProduct = checkoutProductId is null
            ? null
            : store.Products.Single(product => product.Id == checkoutProductId);
        var checkout = checkoutProduct is null
            ? null
            : new EmployeeTaskTarget(
                checkoutProduct.Id,
                checkoutProduct.Name,
                runtime.ActiveCheckout?.RemainingMinutes ?? _options.BaseCheckoutMinutes);

        var inboundOrder = _game.GetProcurementSnapshot().PendingOrders
            .Where(order => order.StoreId == runtime.StoreId)
            .OrderBy(order => order.RemainingMinutes)
            .ThenBy(order => order.OrderId)
            .FirstOrDefault();
        var inboundProduct = inboundOrder is null
            ? null
            : store.Products.Single(product => product.Id == inboundOrder.ProductId);
        var restock = inboundOrder is null || inboundProduct is null
            ? null
            : new EmployeeTaskTarget(
                inboundProduct.ShelfKind,
                inboundProduct.Name,
                inboundOrder.RemainingMinutes);

        var clean = runtime.CleanlinessPermille >= 1_000
            ? null
            : new EmployeeTaskTarget(
                runtime.StoreId,
                $"{store.Name}地面",
                DivideRoundUp(
                    1_000 - runtime.CleanlinessPermille,
                    _options.CleanerBaseRecoveryPermille));
        var customerService = new EmployeeTaskTarget(
            runtime.StoreId,
            "店内顾客",
            1);
        return new StoreTaskDemand(checkout, restock, clean, customerService);
    }

    private IReadOnlyDictionary<string, EmployeeTaskSnapshot> AdjustTaskDurations(
        StoreRuntime runtime,
        IReadOnlyDictionary<string, EmployeeTaskSnapshot> planned)
    {
        var adjusted = new Dictionary<string, EmployeeTaskSnapshot>(planned, StringComparer.Ordinal);
        foreach (var pair in planned)
        {
            var employee = runtime.Employees.Single(item => item.Id.Value == pair.Key);
            if (pair.Value.Kind == EmployeeTaskKind.Checkout
                && runtime.ActiveCheckout is null)
            {
                adjusted[pair.Key] = pair.Value with
                {
                    RemainingMinutes = employee.CalculateTaskMinutes(_options.BaseCheckoutMinutes)
                };
            }
            else if (pair.Value.Kind == EmployeeTaskKind.Clean)
            {
                var recovery = CalculateCleanerRecovery(employee);
                adjusted[pair.Key] = pair.Value with
                {
                    RemainingMinutes = DivideRoundUp(
                        1_000 - runtime.CleanlinessPermille,
                        recovery)
                };
            }
        }

        return adjusted;
    }

    private int CalculateCleanerRecovery(Employee cleaner)
    {
        var scaledRecovery = checked(
            (long)_options.CleanerBaseRecoveryPermille
            * cleaner.EffectiveEfficiencyPermille
            / 1_000L);
        return checked((int)Math.Clamp(scaledRecovery, 1L, 1_000L));
    }

    private static int DivideRoundUp(int dividend, int divisor) =>
        checked((dividend + divisor - 1) / divisor);

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

    private void ProcessVisitorAndQueuePurchase(StoreRuntime runtime)
    {
        var store = _game.GetSnapshot().Stores.Single(snapshot => snapshot.Id == runtime.StoreId);
        var growth = store.Growth ?? _game.GetStoreGrowthSnapshot(runtime.StoreId);
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
            CalculateEffectiveQueueLength(runtime.QueueLength, growth.QueueComfortCapacity),
            runtime.CleanlinessPermille,
            CurrentMinuteOfDay,
            promotionBasisPoints: growth.PromotionPurchaseBonusBasisPoints));
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

    private CommercialStreetSnapshot CreateStreetSnapshot(
        BusinessSnapshot business,
        IReadOnlyList<StoreOperationsSnapshot> storeOperations) =>
        _streetTraffic.CreateSnapshot(
            _clock.GameMinute,
            business.PlayerLevel,
            storeOperations.Select(operations =>
            {
                var store = business.Stores.Single(item => item.Id == operations.StoreId);
                return new StreetStoreDemand(
                    store.Id,
                    store.Name,
                    operations.ArrivalDemand.FinalBasisPoints);
            }));

    private DemandBreakdown CalculateArrivalDemand(
        StoreRuntime runtime,
        BusinessStoreSnapshot store)
    {
        var growth = store.Growth ?? _game.GetStoreGrowthSnapshot(runtime.StoreId);
        return DemandModel.CalculateArrival(new DemandContext(
            _options.BaseArrivalBasisPoints,
            CalculateAveragePriceIndex(store.Products),
            runtime.ServicePermille,
            CalculateEffectiveQueueLength(runtime.QueueLength, growth.QueueComfortCapacity),
            runtime.CleanlinessPermille,
            CurrentMinuteOfDay,
            growth.AttractionBonusBasisPoints,
            growth.PromotionArrivalBonusBasisPoints));
    }

    private static int CalculateEffectiveQueueLength(int queueLength, int comfortCapacity) =>
        Math.Max(0, queueLength - comfortCapacity);

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
            DayStartOperatingCostCents = saved.DayStartOperatingCostCents;
            DayQueueLengthTotal = saved.DayQueueLengthTotal;
            DayTickCount = saved.DayTickCount;
        }

        public string StoreId { get; }

        public Employee[] Employees { get; set; }

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
                DayTickCount,
                DayStartOperatingCostCents);

        private int DayStartVisitors { get; set; }

        private int DayStartAcceptedPurchases { get; set; }

        private int DayStartCompletedSales { get; set; }

        private int DayStartLostSales { get; set; }

        private long DayStartRevenueCents { get; set; }

        private long DayStartGrossProfitCents { get; set; }

        private long DayStartWageCostCents { get; set; }

        private long DayStartOperatingCostCents { get; set; }

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
            var operatingCost = checked(store.OperatingCostCents - DayStartOperatingCostCents);
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
                checked(grossProfit - wageCost - operatingCost),
                CleanlinessPermille,
                averageQueue,
                operatingCost);
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
            DayStartOperatingCostCents = store.OperatingCostCents;
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
