using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class DesktopShopWindowRenderingTests
{
    [Fact]
    public void Content_UsesOneFlattenedSurfaceAndThreeAccessibleHitTargets()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new DesktopShopWindow(new MarketViewModel(MarketTestSession.Create()));
                var root = Assert.IsType<Grid>(window.Content);

                var surface = Assert.IsType<BusinessDesktopShopSurfaceControl>(root.Children[0]);
                Assert.True(surface.UsesLogicalPixelScaling);
                var hitTargets = Assert.IsType<Canvas>(root.Children[1]);
                Assert.Equal(3, hitTargets.Children.Count);

                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "WPF verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    [Fact]
    public void DesktopAndManagementWindows_DoNotCreateTaskbarButtons()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var viewModel = new MarketViewModel(MarketTestSession.Create());
                var desktop = new DesktopShopWindow(viewModel);
                var management = new ManagementWindow(viewModel);

                Assert.False(desktop.ShowInTaskbar);
                Assert.False(management.ShowInTaskbar);

                management.Close();
                desktop.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "WPF verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
