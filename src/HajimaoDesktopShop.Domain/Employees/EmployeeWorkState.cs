using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Employees;

public sealed record EmployeeWorkState(
    int WorkedMinutes,
    Money TotalWagesAccrued,
    long WageRemainderCents);
