namespace HajimaoDesktopShop.Desktop.Services;

public static class RefreshCadencePolicy
{
    private static readonly TimeSpan PresentationInterval = TimeSpan.FromMilliseconds(125);

    public static TimeSpan GetInterval(bool managementOpen) =>
        PresentationInterval;
}
