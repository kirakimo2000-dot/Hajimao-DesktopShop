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
        var visible = employees
            .OrderBy(employee => DutyDisplayOrder(
                employee.CurrentTask?.Kind ?? EmployeeTaskKind.Idle))
            .ThenBy(employee => employee.EmployeeId, StringComparer.Ordinal)
            .Take(4)
            .ToArray();
        var poses = new BusinessShopEmployeePose[visible.Length];
        for (var index = 0; index < visible.Length; index++)
        {
            poses[index] = CreatePose(visible[index], index, animationFrame, reduceMotion);
        }

        return Array.AsReadOnly(poses);
    }

    private static BusinessShopEmployeePose CreatePose(
        EmployeeOperationsEmployeeSnapshot employee,
        int index,
        int animationFrame,
        bool reduceMotion)
    {
        var taskKind = employee.CurrentTask?.Kind ?? EmployeeTaskKind.Idle;
        var targetKey = employee.CurrentTask?.TargetKey;
        var x = taskKind switch
        {
            EmployeeTaskKind.Checkout => Move(animationFrame, index, 350, 362, 2, reduceMotion),
            EmployeeTaskKind.Restock => Move(
                animationFrame,
                index,
                30,
                RestockAnchor(targetKey),
                4,
                reduceMotion),
            EmployeeTaskKind.Clean => Move(animationFrame, index, 30, 318, 4, reduceMotion),
            EmployeeTaskKind.CustomerService => Move(
                animationFrame,
                index,
                112,
                280,
                4,
                reduceMotion),
            EmployeeTaskKind.Rest => 18 + (index * 24),
            _ => IdleAnchor(employee.Role, index)
        };
        return new BusinessShopEmployeePose(
            employee.EmployeeId,
            employee.Role,
            x,
            142,
            IsSupporting: taskKind == EmployeeTaskKind.CustomerService,
            taskKind,
            targetKey);
    }

    private static int Move(
        int animationFrame,
        int index,
        int start,
        int end,
        int step,
        bool reduceMotion) =>
        CharacterMotion.PingPong(
            animationFrame,
            index * 7,
            start,
            end,
            step,
            reduceMotion);

    private static int RestockAnchor(string? targetKey) => targetKey?.ToLowerInvariant() switch
    {
        "ambient" => 94,
        "chilled" => 198,
        "frozen" => 302,
        _ => 230
    };

    private static int IdleAnchor(EmployeeRole role, int index) => role switch
    {
        EmployeeRole.Cashier => 356,
        EmployeeRole.Restocker => 30,
        _ => 112 + (index * 40)
    };

    private static int DutyDisplayOrder(EmployeeTaskKind kind) => kind switch
    {
        EmployeeTaskKind.Checkout => 0,
        EmployeeTaskKind.Restock => 1,
        EmployeeTaskKind.Clean => 2,
        EmployeeTaskKind.CustomerService => 3,
        EmployeeTaskKind.Rest => 4,
        _ => 5
    };
}
