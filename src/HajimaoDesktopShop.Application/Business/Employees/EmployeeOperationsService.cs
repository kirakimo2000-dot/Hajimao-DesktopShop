using System.Globalization;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed class EmployeeOperationsService
{
    private const int CandidatePoolSize = 3;
    private const int DefaultShiftStartMinute = 480;
    private const int DefaultShiftEndMinute = 960;
    private const ulong SplitMixIncrement = 0x9E3779B97F4A7C15UL;

    private static readonly EmployeeRole[] CandidateRoles =
        [
            EmployeeRole.Cashier,
            EmployeeRole.Restocker,
            EmployeeRole.SalesAssistant,
            EmployeeRole.Cleaner,
            EmployeeRole.Manager,
            EmployeeRole.Buyer
        ];

    private static readonly EmployeeProfileDefinition[] DefaultProfiles =
        [
            new("legacy-xiaokui", "小葵", "legacy", "employee-a01", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-xiaoman", "小满", "legacy", "employee-a02", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-acheng", "阿澄", "legacy", "employee-a03", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-taozi", "桃子", "legacy", "employee-a04", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-qingchuan", "晴川", "legacy", "employee-b01", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-anhe", "安禾", "legacy", "employee-b02", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-linglan", "铃兰", "legacy", "employee-b03", CandidateRoles, 1000, 1000, "社区门店经验。"),
            new("legacy-xingye", "星野", "legacy", "employee-b04", CandidateRoles, 1000, 1000, "社区门店经验。")
        ];

    private readonly object _gate = new();
    private readonly IEmployeeOperationsGateway _gateway;
    private readonly Dictionary<string, EmployeeCandidate> _candidates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Employee> _employees = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _storeByEmployee = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _profileByEmployee = new(StringComparer.Ordinal);
    private readonly EmployeeRoster _roster = new();
    private readonly IReadOnlyList<EmployeeProfileDefinition> _profiles;
    private ulong _candidateRandomState;
    private long _nextCandidateId;

    public EmployeeOperationsService(
        IEmployeeOperationsGateway gateway,
        ulong candidateRandomState = 0x48414A494D414FUL,
        long nextCandidateId = 1,
        IEnumerable<EmployeeCandidate>? candidates = null,
        IEnumerable<EmployeeProfileDefinition>? profiles = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        if (nextCandidateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextCandidateId));
        }

        _gateway = gateway;
        _candidateRandomState = candidateRandomState;
        _nextCandidateId = nextCandidateId;
        _profiles = Array.AsReadOnly((profiles ?? DefaultProfiles).ToArray());
        if (_profiles.Count == 0)
        {
            throw new ArgumentException("At least one employee profile is required.", nameof(profiles));
        }

        if (candidates is null)
        {
            RefreshCandidatesCore();
            return;
        }

        foreach (var candidate in candidates)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            if (!_candidates.TryAdd(candidate.CandidateId, candidate))
            {
                throw new ArgumentException(
                    $"Candidate '{candidate.CandidateId}' is duplicated.",
                    nameof(candidates));
            }
        }
    }

    public EmployeeOperationsSnapshot GetSnapshot() => GetSnapshot(null);

    internal EmployeeOperationsSnapshot GetSnapshot(
        IReadOnlyDictionary<string, EmployeeTaskSnapshot>? currentTasks)
    {
        lock (_gate)
        {
            var candidates = _candidates.Values
                .OrderBy(candidate => candidate.CandidateId, StringComparer.Ordinal)
                .ToArray();
            var employees = _employees.Values
                .OrderBy(employee => employee.Id.Value, StringComparer.Ordinal)
                .Select(employee =>
                {
                    var shift = _roster.GetShift(employee.Id.Value)
                        ?? throw new InvalidOperationException("Every registered employee must have a shift.");
                    var employeeId = employee.Id.Value;
                    var currentTask = currentTasks?.GetValueOrDefault(employeeId)
                        ?? new EmployeeTaskSnapshot(EmployeeTaskKind.Idle, null, null, null);
                    return new EmployeeOperationsEmployeeSnapshot(
                        employeeId,
                        employee.Name,
                        employee.Role,
                        employee.EfficiencyPermille,
                        employee.EffectiveEfficiencyPermille,
                        employee.HourlyWage.Cents,
                        employee.TrainingLevel,
                        employee.EnergyPermille,
                        employee.SatisfactionPermille,
                        _storeByEmployee[employee.Id.Value],
                        shift.StartMinute,
                        shift.EndMinute,
                        shift.IsAlwaysOn,
                        currentTask,
                        EmployeeTaskPriorityCatalog.GetPriorities(employee.Role),
                        _profileByEmployee[employee.Id.Value]);
                })
                .ToArray();
            return new EmployeeOperationsSnapshot(
                _candidateRandomState,
                _nextCandidateId,
                Array.AsReadOnly(candidates),
                Array.AsReadOnly(employees));
        }
    }

    public void RefreshCandidates()
    {
        lock (_gate)
        {
            RefreshCandidatesCore();
        }
    }

    public EmployeeCommandResult Hire(string candidateId, string storeId)
    {
        var normalizedCandidateId = NormalizeId(candidateId, nameof(candidateId));
        var normalizedStoreId = NormalizeId(storeId, nameof(storeId));
        lock (_gate)
        {
            if (!_candidates.TryGetValue(normalizedCandidateId, out var candidate))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownCandidate,
                    null,
                    Money.Zero);
            }

            if (!_gateway.IsStoreOpen(normalizedStoreId))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownStore,
                    null,
                    candidate.HireCost);
            }

            var employeeId = CreateEmployeeId(candidate.CandidateId);
            if (_employees.ContainsKey(employeeId))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.DuplicateEmployee,
                    employeeId,
                    Money.Zero);
            }

            if (!_gateway.TryDebitEmployeeExpense(candidate.HireCost))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.InsufficientFunds,
                    null,
                    candidate.HireCost);
            }

            var employee = new Employee(
                new EmployeeId(employeeId),
                candidate.Name,
                candidate.Role,
                candidate.EfficiencyPermille,
                candidate.HourlyWage);
            _employees.Add(employeeId, employee);
            _storeByEmployee.Add(employeeId, normalizedStoreId);
            _profileByEmployee.Add(employeeId, candidate.ProfileId);
            _roster.SetShift(new EmployeeShift(
                employeeId,
                normalizedStoreId,
                DefaultShiftStartMinute,
                DefaultShiftEndMinute));
            _candidates.Remove(normalizedCandidateId);
            return new EmployeeCommandResult(
                EmployeeCommandStatus.Success,
                employeeId,
                candidate.HireCost);
        }
    }

    public void RegisterExistingEmployee(
        string storeId,
        Employee employee,
        EmployeeShift? shift = null,
        string profileId = "legacy")
    {
        var normalizedStoreId = NormalizeId(storeId, nameof(storeId));
        ArgumentNullException.ThrowIfNull(employee);
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile ID is required.", nameof(profileId));
        }
        lock (_gate)
        {
            var employeeId = employee.Id.Value;
            if (_employees.ContainsKey(employeeId))
            {
                throw new ArgumentException(
                    $"Employee '{employeeId}' cannot be registered more than once.",
                    nameof(employee));
            }

            var registeredShift = shift
                ?? EmployeeShift.CreateLegacyAlwaysOn(employeeId, normalizedStoreId);
            if (!string.Equals(registeredShift.EmployeeId, employeeId, StringComparison.Ordinal)
                || !string.Equals(registeredShift.StoreId, normalizedStoreId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The restored shift must match the employee and store assignment.",
                    nameof(shift));
            }

            _employees.Add(employeeId, employee);
            _storeByEmployee.Add(employeeId, normalizedStoreId);
            _profileByEmployee.Add(employeeId, profileId.Trim());
            _roster.SetShift(registeredShift);
        }
    }

    public EmployeeCommandResult Train(string employeeId)
    {
        var normalizedEmployeeId = NormalizeId(employeeId, nameof(employeeId));
        lock (_gate)
        {
            if (!_employees.TryGetValue(normalizedEmployeeId, out var employee))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownEmployee,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            if (employee.TrainingLevel >= 5)
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.MaximumTraining,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            var cost = employee.HourlyWage * checked((employee.TrainingLevel + 1) * 8);
            if (!_gateway.TryDebitEmployeeExpense(cost))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.InsufficientFunds,
                    normalizedEmployeeId,
                    cost);
            }

            employee.CompleteTraining();
            return new EmployeeCommandResult(
                EmployeeCommandStatus.Success,
                normalizedEmployeeId,
                cost);
        }
    }

    public EmployeeCommandResult AssignStore(string employeeId, string storeId)
    {
        var normalizedEmployeeId = NormalizeId(employeeId, nameof(employeeId));
        var normalizedStoreId = NormalizeId(storeId, nameof(storeId));
        lock (_gate)
        {
            if (!_employees.ContainsKey(normalizedEmployeeId))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownEmployee,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            if (!_gateway.IsStoreOpen(normalizedStoreId))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownStore,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            var current = _roster.GetShift(normalizedEmployeeId)
                ?? throw new InvalidOperationException("Every registered employee must have a shift.");
            _storeByEmployee[normalizedEmployeeId] = normalizedStoreId;
            _roster.SetShift(current.IsAlwaysOn
                ? EmployeeShift.CreateLegacyAlwaysOn(normalizedEmployeeId, normalizedStoreId)
                : new EmployeeShift(
                    normalizedEmployeeId,
                    normalizedStoreId,
                    current.StartMinute,
                    current.EndMinute));
            return new EmployeeCommandResult(
                EmployeeCommandStatus.Success,
                normalizedEmployeeId,
                Money.Zero);
        }
    }

    public EmployeeCommandResult SetShift(string employeeId, int startMinute, int endMinute)
    {
        var normalizedEmployeeId = NormalizeId(employeeId, nameof(employeeId));
        lock (_gate)
        {
            if (!_employees.ContainsKey(normalizedEmployeeId))
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.UnknownEmployee,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            try
            {
                _roster.SetShift(new EmployeeShift(
                    normalizedEmployeeId,
                    _storeByEmployee[normalizedEmployeeId],
                    startMinute,
                    endMinute));
            }
            catch (ArgumentOutOfRangeException)
            {
                return new EmployeeCommandResult(
                    EmployeeCommandStatus.InvalidShift,
                    normalizedEmployeeId,
                    Money.Zero);
            }

            return new EmployeeCommandResult(
                EmployeeCommandStatus.Success,
                normalizedEmployeeId,
                Money.Zero);
        }
    }

    internal IReadOnlyList<EmployeeRuntimeAssignment> GetRuntimeAssignments()
    {
        lock (_gate)
        {
            return _employees.Values
                .OrderBy(employee => employee.Id.Value, StringComparer.Ordinal)
                .Select(employee => new EmployeeRuntimeAssignment(
                    _storeByEmployee[employee.Id.Value],
                    employee,
                    _roster.GetShift(employee.Id.Value)
                        ?? throw new InvalidOperationException("Every registered employee must have a shift.")))
                .ToArray();
        }
    }

    internal IReadOnlyList<Employee> ResolveAvailableEmployees(
        string storeId,
        int localMinute)
    {
        var normalizedStoreId = NormalizeId(storeId, nameof(storeId));
        lock (_gate)
        {
            var available = new List<Employee>();
            foreach (var employee in _employees.Values
                         .Where(employee => string.Equals(
                             _storeByEmployee[employee.Id.Value],
                             normalizedStoreId,
                             StringComparison.Ordinal))
                         .OrderBy(employee => employee.Id.Value, StringComparer.Ordinal))
            {
                if (!_roster.IsScheduled(employee.Id.Value, normalizedStoreId, localMinute))
                {
                    employee.RecordRestMinute();
                    continue;
                }

                if (employee.CanWork)
                {
                    available.Add(employee);
                    continue;
                }

                employee.RecordRestMinute();
            }

            return available;
        }
    }

    internal void RecordPaidWorkCondition(EmployeeId employeeId)
    {
        lock (_gate)
        {
            if (!_employees.TryGetValue(employeeId.Value, out var employee))
            {
                throw new KeyNotFoundException($"Employee '{employeeId.Value}' was not found.");
            }

            employee.RecordWorkedConditionMinute();
        }
    }

    private void RefreshCandidatesCore()
    {
        _candidates.Clear();
        for (var index = 0; index < CandidatePoolSize; index++)
        {
            var candidate = GenerateCandidate();
            _candidates.Add(candidate.CandidateId, candidate);
        }
    }

    private EmployeeCandidate GenerateCandidate()
    {
        var candidateNumber = _nextCandidateId;
        _nextCandidateId = checked(_nextCandidateId + 1L);
        var profile = _profiles[NextCandidateInt(_profiles.Count)];
        var role = profile.AllowedRoles[NextCandidateInt(profile.AllowedRoles.Count)];
        var baseEfficiency = 800 + (NextCandidateInt(21) * 25);
        var efficiency = Math.Max(1, checked(baseEfficiency * profile.EfficiencyBiasPermille / 1000));
        var roleWage = role switch
        {
            EmployeeRole.Manager => 900,
            EmployeeRole.Buyer => 800,
            EmployeeRole.Restocker => 700,
            EmployeeRole.Cleaner => 600,
            _ => 650
        };
        var performancePremium = Math.Max(0, efficiency - 800) / 25 * 25L;
        var hourlyWage = new Money(Math.Max(
            1,
            checked((roleWage + performancePremium) * profile.WageBiasPermille / 1000)));
        return new EmployeeCandidate(
            $"candidate-{candidateNumber:D6}",
            profile.DisplayName,
            role,
            efficiency,
            hourlyWage,
            profile.Id);
    }

    private int NextCandidateInt(int exclusiveMaximum)
    {
        _candidateRandomState = unchecked(_candidateRandomState + SplitMixIncrement);
        var mixed = _candidateRandomState;
        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;
        mixed ^= mixed >> 31;
        return checked((int)(mixed % (uint)exclusiveMaximum));
    }

    private static string CreateEmployeeId(string candidateId)
    {
        const string prefix = "candidate-";
        if (candidateId.StartsWith(prefix, StringComparison.Ordinal)
            && long.TryParse(
                candidateId.AsSpan(prefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericId)
            && numericId > 0)
        {
            return $"employee-{numericId:D6}";
        }

        return $"employee-{candidateId}";
    }

    private static string NormalizeId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ID is required.", parameterName);
        }

        return value.Trim();
    }
}
