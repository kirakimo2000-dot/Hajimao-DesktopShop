using System.Runtime.ExceptionServices;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class TrayIconServiceTests
{
    [Fact]
    public void Lifecycle_ShowsOneIconUntilDisposed()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var service = new TrayIconService();
                Assert.True(service.IsVisible);

                service.Dispose();

                Assert.False(service.IsVisible);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "Tray verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
