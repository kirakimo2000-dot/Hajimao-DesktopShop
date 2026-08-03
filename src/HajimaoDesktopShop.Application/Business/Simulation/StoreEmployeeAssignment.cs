using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed record StoreEmployeeAssignment
{
    public StoreEmployeeAssignment(string storeId, Employee employee)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        ArgumentNullException.ThrowIfNull(employee);
        StoreId = storeId.Trim();
        Employee = employee;
    }

    public string StoreId { get; }

    public Employee Employee { get; }
}
