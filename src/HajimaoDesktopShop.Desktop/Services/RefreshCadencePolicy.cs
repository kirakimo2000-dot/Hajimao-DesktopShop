namespace HajimaoDesktopShop.Desktop.Services;

public static class RefreshCadencePolicy
{
    private static readonly TimeSpan PresentationInterval = TimeSpan.FromSeconds(1d / 24d);

    public static TimeSpan GetInterval(bool managementOpen) =>
        PresentationInterval;
}
