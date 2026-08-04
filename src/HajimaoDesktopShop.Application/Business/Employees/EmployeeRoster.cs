namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed class EmployeeRoster
{
    private readonly Dictionary<string, EmployeeShift> _shifts = new(StringComparer.Ordinal);

    public IReadOnlyList<EmployeeShift> Shifts =>
        _shifts.Values
            .OrderBy(shift => shift.EmployeeId, StringComparer.Ordinal)
            .ToArray();

    public void SetShift(EmployeeShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);
        _shifts[shift.EmployeeId] = shift;
    }

    public EmployeeShift? GetShift(string employeeId)
    {
        var normalizedEmployeeId = NormalizeId(employeeId, nameof(employeeId));
        return _shifts.GetValueOrDefault(normalizedEmployeeId);
    }

    public bool IsScheduled(string employeeId, string storeId, int localMinute)
    {
        var shift = GetShift(employeeId);
        var normalizedStoreId = NormalizeId(storeId, nameof(storeId));
        return shift is not null
            && string.Equals(shift.StoreId, normalizedStoreId, StringComparison.Ordinal)
            && shift.ContainsMinute(localMinute);
    }

    public bool RemoveShift(string employeeId)
    {
        var normalizedEmployeeId = NormalizeId(employeeId, nameof(employeeId));
        return _shifts.Remove(normalizedEmployeeId);
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
