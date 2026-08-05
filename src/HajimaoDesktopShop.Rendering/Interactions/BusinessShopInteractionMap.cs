namespace HajimaoDesktopShop.Rendering.Interactions;

public static class BusinessShopInteractionMap
{
    private static readonly IReadOnlyList<BusinessShopInteractionTarget> Shelves =
        Array.AsReadOnly(new[]
        {
            Shelf("ambient", 62),
            Shelf("chilled", 166),
            Shelf("frozen", 270)
        });

    public static IReadOnlyList<BusinessShopInteractionTarget> CreateTargets(
        BusinessShopSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var employees = frame.Snapshot.Employees.Employees
            .Where(employee => employee.StoreId == frame.StoreId)
            .ToArray();
        var employeeTargets = BusinessShopEmployeeChoreography.CreatePoses(
                employees,
                frame.AnimationFrame,
                frame.ReduceMotion)
            .Select(pose => new BusinessShopInteractionTarget(
                BusinessShopInteractionKind.Employee,
                pose.EmployeeId,
                new LogicalPixelRect(pose.X - 12, pose.Y - 32, 24, 34)));
        return Array.AsReadOnly(Shelves.Concat(employeeTargets).ToArray());
    }

    public static BusinessShopInteractionTarget? HitTest(
        BusinessShopSceneFrame frame,
        int logicalX,
        int logicalY)
    {
        var targets = CreateTargets(frame);
        return targets
                .Where(target => target.Kind == BusinessShopInteractionKind.Employee)
                .Reverse()
                .FirstOrDefault(target => target.Bounds.Contains(logicalX, logicalY))
            ?? targets.FirstOrDefault(target =>
                target.Kind == BusinessShopInteractionKind.Shelf
                && target.Bounds.Contains(logicalX, logicalY));
    }

    private static BusinessShopInteractionTarget Shelf(string key, int x) =>
        new(
            BusinessShopInteractionKind.Shelf,
            key,
            new LogicalPixelRect(x, 52, 96, 68));
}
