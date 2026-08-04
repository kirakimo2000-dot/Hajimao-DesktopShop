namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed record EmployeeShift
{
    public const int MinutesPerDay = 1_440;
    public const int MaximumShiftMinutes = 480;

    public EmployeeShift(string employeeId, string storeId, int startMinute, int endMinute)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            throw new ArgumentException("Employee ID is required.", nameof(employeeId));
        }

        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        ValidateMinute(startMinute, nameof(startMinute));
        ValidateMinute(endMinute, nameof(endMinute));

        var durationMinutes = endMinute > startMinute
            ? endMinute - startMinute
            : MinutesPerDay - startMinute + endMinute;
        if (durationMinutes is <= 0 or > MaximumShiftMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endMinute),
                $"A shift must last between 1 and {MaximumShiftMinutes} minutes.");
        }

        EmployeeId = employeeId.Trim();
        StoreId = storeId.Trim();
        StartMinute = startMinute;
        EndMinute = endMinute;
        DurationMinutes = durationMinutes;
    }

    public string EmployeeId { get; }

    public string StoreId { get; }

    public int StartMinute { get; }

    public int EndMinute { get; }

    public int DurationMinutes { get; }

    public bool ContainsMinute(int minute)
    {
        ValidateMinute(minute, nameof(minute));
        return EndMinute > StartMinute
            ? minute >= StartMinute && minute < EndMinute
            : minute >= StartMinute || minute < EndMinute;
    }

    private static void ValidateMinute(int minute, string parameterName)
    {
        if (minute is < 0 or >= MinutesPerDay)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
