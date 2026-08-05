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
    public void Content_DefaultsToAOneStoreStreetAndKeepsTheShopSurfaceAvailable()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var viewModel = new MarketViewModel(MarketTestSession.Create());
                var window = new DesktopShopWindow(viewModel);
                Assert.Equal(ProductIdentity.DesktopWindowTitle, window.Title);
                var root = Assert.IsType<Grid>(window.Content);
                Assert.Equal(4, root.ContextMenu?.Items.Count);
                Assert.Equal(248, window.Width);
                Assert.Equal(180, window.Height);

                var streetPage = Assert.IsType<Grid>(root.FindName("StreetPage"));
                var street = Assert.IsType<CommercialStreetSceneControl>(root.FindName("StreetScene"));
                Assert.True(street.UsesLogicalPixelScaling);
                Assert.Equal(System.Windows.Visibility.Visible, streetPage.Visibility);

                var storePage = Assert.IsType<Grid>(root.FindName("StorePage"));
                var surface = Assert.IsType<BusinessDesktopShopSurfaceControl>(root.FindName("StoreSurface"));
                Assert.True(surface.UsesLogicalPixelScaling);
                Assert.Equal(System.Windows.Visibility.Collapsed, storePage.Visibility);

                viewModel.DesktopNavigation.OpenStoreCommand.Execute("corner-store");
                streetPage.GetBindingExpression(Grid.VisibilityProperty)?.UpdateTarget();
                storePage.GetBindingExpression(Grid.VisibilityProperty)?.UpdateTarget();

                Assert.Equal(420, window.Width);
                Assert.Equal(280, window.Height);
                Assert.Equal(System.Windows.Visibility.Collapsed, streetPage.Visibility);
                Assert.Equal(System.Windows.Visibility.Visible, storePage.Visibility);

                viewModel.DesktopNavigation.BackToStreetCommand.Execute(null);
                Assert.Equal(248, window.Width);
                Assert.Equal(180, window.Height);

                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "WPF verification thread timed out.");
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "WPF verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
