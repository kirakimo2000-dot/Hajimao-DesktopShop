namespace HajimaoDesktopShop.Desktop.Services;

public static class RefreshCadencePolicy
{
    private static readonly TimeSpan DesktopOnlyInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ManagementOpenInterval = TimeSpan.FromMilliseconds(250);

    public static TimeSpan GetInterval(bool managementOpen) =>
        managementOpen ? ManagementOpenInterval : DesktopOnlyInterval;
}
