using HajimaoDesktopShop.Application.Business.Employees;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

internal static class EmployeeTaskTextFormatter
{
    public static string FormatTask(EmployeeTaskSnapshot? task)
    {
        if (task is null || task.Kind == EmployeeTaskKind.Idle)
        {
            return task?.TargetName is { Length: > 0 } idleReason
                ? $"待命 · {idleReason}"
                : "待命";
        }

        var label = Label(task.Kind);
        var target = task.TargetName is { Length: > 0 } targetName
            ? $" · {targetName}"
            : string.Empty;
        var remaining = task.Kind is EmployeeTaskKind.Checkout
                or EmployeeTaskKind.Restock
                or EmployeeTaskKind.Clean
            && task.RemainingMinutes is > 0
                ? $" · 剩余 {task.RemainingMinutes} 分钟"
                : string.Empty;
        return $"{label}{target}{remaining}";
    }

    public static string FormatPriorities(IReadOnlyList<EmployeeTaskKind>? priorities)
    {
        if (priorities is null || priorities.Count == 0)
        {
            return "优先级 待命";
        }

        return $"优先级 {string.Join(" → ", priorities.Select(Label))}";
    }

    private static string Label(EmployeeTaskKind kind) => kind switch
    {
        EmployeeTaskKind.Checkout => "收银",
        EmployeeTaskKind.Restock => "补货",
        EmployeeTaskKind.Clean => "清洁",
        EmployeeTaskKind.CustomerService => "导购",
        EmployeeTaskKind.Rest => "休息",
        _ => "待命"
    };
}
