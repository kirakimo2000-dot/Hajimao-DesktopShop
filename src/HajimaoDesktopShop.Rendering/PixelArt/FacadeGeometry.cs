namespace HajimaoDesktopShop.Rendering.PixelArt;

public enum FacadeGeometryKind
{
    Convenience,
    Commuter,
    Discount,
    Warehouse,
    Lifestyle,
    Health,
    Premium,
    Variety
}

public sealed record FacadeGeometry(
    int TopOffset,
    int SideInset,
    int SignTop,
    int SignHeight,
    int WindowBayCount,
    bool DoorOnRight,
    bool HasCornice,
    bool HasVerticalSign);

public static class FacadeGeometryPolicy
{
    public static FacadeGeometry Resolve(FacadeGeometryKind kind) => kind switch
    {
        FacadeGeometryKind.Convenience => new(10, 0, 7, 15, 3, true, false, false),
        FacadeGeometryKind.Commuter => new(16, 6, 5, 13, 2, false, false, true),
        FacadeGeometryKind.Discount => new(6, 0, 13, 18, 4, true, true, false),
        FacadeGeometryKind.Warehouse => new(2, 0, 20, 15, 2, true, true, false),
        FacadeGeometryKind.Lifestyle => new(12, 10, 6, 17, 3, false, false, true),
        FacadeGeometryKind.Health => new(8, 4, 8, 14, 3, true, true, true),
        FacadeGeometryKind.Premium => new(0, 18, 9, 18, 2, false, true, false),
        FacadeGeometryKind.Variety => new(5, 2, 6, 22, 5, true, false, false),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
