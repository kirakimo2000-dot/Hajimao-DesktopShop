namespace HajimaoDesktopShop.Desktop.Services;

public static class RefreshCadencePolicy
{
    private const int PresentationFramesPerManagementRefresh = 24;
    private static readonly TimeSpan PresentationInterval = TimeSpan.FromSeconds(1d / 24d);

    public static TimeSpan GetInterval(bool managementOpen) =>
        PresentationInterval;

    public static bool IsManagementRefresh(int presentationTick) =>
        presentationTick > 0
        && presentationTick % PresentationFramesPerManagementRefresh == 0;
}
