namespace HajimaoDesktopShop.Domain.Employees;

public readonly record struct EmployeeId
{
    public EmployeeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Employee ID is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
