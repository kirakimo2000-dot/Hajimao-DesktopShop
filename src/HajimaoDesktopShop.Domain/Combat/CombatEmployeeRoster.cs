namespace HajimaoDesktopShop.Domain.Combat;

public sealed record CombatEmployeeInstance
{
    public CombatEmployeeInstance(
        string instanceId,
        string characterId,
        string displayName,
        string? assignedStoreId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        InstanceId = instanceId.Trim();
        CharacterId = characterId.Trim();
        DisplayName = displayName.Trim();
        AssignedStoreId = string.IsNullOrWhiteSpace(assignedStoreId) ? null : assignedStoreId.Trim();
    }

    public string InstanceId { get; }
    public string CharacterId { get; }
    public string DisplayName { get; }
    public string? AssignedStoreId { get; init; }
}

public sealed class CombatEmployeeRoster
{
    private readonly IReadOnlyList<CombatEmployeeInstance> _employees;

    public CombatEmployeeRoster(IEnumerable<CombatEmployeeInstance> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);
        var materialized = employees.ToArray();
        if (materialized.Select(item => item.InstanceId).Distinct(StringComparer.Ordinal).Count() != materialized.Length)
        {
            throw new ArgumentException("Employee instance IDs must be unique.", nameof(employees));
        }

        if (materialized.Where(item => item.AssignedStoreId is not null)
            .Select(item => item.AssignedStoreId)
            .Distinct(StringComparer.Ordinal).Count()
            != materialized.Count(item => item.AssignedStoreId is not null))
        {
            throw new ArgumentException("A store can only have one assigned employee.", nameof(employees));
        }

        _employees = Array.AsReadOnly(materialized);
    }

    public IReadOnlyList<CombatEmployeeInstance> Employees => _employees;

    public IReadOnlyList<CombatEmployeeInstance> AssignedTo(string storeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        return _employees.Where(item => string.Equals(item.AssignedStoreId, storeId, StringComparison.Ordinal)).ToArray();
    }

    public CombatEmployeeRoster Assign(string instanceId, string storeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        if (!_employees.Any(item => string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal)))
        {
            throw new KeyNotFoundException($"Employee '{instanceId}' is not owned.");
        }

        return new CombatEmployeeRoster(_employees.Select(item =>
        {
            if (string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal))
            {
                return item with { AssignedStoreId = storeId.Trim() };
            }

            return string.Equals(item.AssignedStoreId, storeId, StringComparison.Ordinal)
                ? item with { AssignedStoreId = null }
                : item;
        }));
    }

    public CombatEmployeeRoster Add(CombatEmployeeInstance employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return new CombatEmployeeRoster([.. _employees, employee]);
    }
}
