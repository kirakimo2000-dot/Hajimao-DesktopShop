using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Interactions;

public static class BusinessShopEmployeeChoreography
{
    public static IReadOnlyList<BusinessShopEmployeePose> CreatePoses(
        IReadOnlyList<EmployeeOperationsEmployeeSnapshot> employees,
        int animationFrame,
        bool reduceMotion)
    {
        ArgumentNullException.ThrowIfNull(employees);
        var poses = new List<BusinessShopEmployeePose>();

        var cashier = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Cashier);
        if (cashier is not null)
        {
            poses.Add(new BusinessShopEmployeePose(
                cashier.EmployeeId,
                cashier.Role,
                CharacterMotion.PingPong(
                    animationFrame,
                    0,
                    350,
                    362,
                    2,
                    reduceMotion),
                142,
                IsSupporting: false));
        }

        var restocker = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Restocker);
        if (restocker is not null)
        {
            poses.Add(new BusinessShopEmployeePose(
                restocker.EmployeeId,
                restocker.Role,
                CharacterMotion.PingPong(
                    animationFrame,
                    9,
                    30,
                    230,
                    4,
                    reduceMotion),
                142,
                IsSupporting: false));
        }

        var supporting = employees
            .Where(employee => employee.Role is not EmployeeRole.Cashier and not EmployeeRole.Restocker)
            .Take(2)
            .ToArray();
        for (var index = 0; index < supporting.Length; index++)
        {
            var employee = supporting[index];
            poses.Add(new BusinessShopEmployeePose(
                employee.EmployeeId,
                employee.Role,
                CharacterMotion.PingPong(
                    animationFrame,
                    index * 11,
                    112 + index * 40,
                    176 + index * 40,
                    4,
                    reduceMotion),
                142,
                IsSupporting: true));
        }

        return Array.AsReadOnly(poses.ToArray());
    }
}
