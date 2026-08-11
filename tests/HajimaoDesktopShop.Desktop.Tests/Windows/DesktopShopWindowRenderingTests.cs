using System.Runtime.ExceptionServices;
using System.IO;
using System.Windows.Controls;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class DesktopShopWindowRenderingTests
{
    [Fact]
    public void DragHandler_PreservesThePositionChosenByDragMove()
    {
        var source = File.ReadAllText(FindDesktopShopWindowCodeBehindPath());
        var handlerStart = source.IndexOf(
            "private void OnSurfaceMouseLeftButtonDown",
            StringComparison.Ordinal);
        var nextHandlerStart = source.IndexOf(
            "private void OnOpenManagementClick",
            handlerStart,
            StringComparison.Ordinal);

        Assert.True(handlerStart >= 0);
        Assert.True(nextHandlerStart > handlerStart);
        var dragHandler = source[handlerStart..nextHandlerStart];
        Assert.Contains("DragMove();", dragHandler, StringComparison.Ordinal);
        Assert.Contains("SnapAboveTaskbarIfNear();", dragHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplySurfaceLayout", dragHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopSurface_RemainsTopmostWhileManagementIsOpen()
    {
        var xaml = File.ReadAllText(FindDesktopShopWindowPath());
        var appSource = File.ReadAllText(FindAppCodeBehindPath());

        Assert.Contains("Topmost=\"True\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("_desktopWindow.Topmost = false", appSource, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationAndStreetGrowth_ResizeWithoutResettingTheHorizontalPosition()
    {
        var source = File.ReadAllText(FindDesktopShopWindowCodeBehindPath());

        Assert.Equal(
            2,
            CountOccurrences(source, "ApplySurfaceLayout(reposition: false);"));
    }

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

    [Fact]
    public void SelectShopObject_OpensReadOnlyOverviewAndRequestsManagementWindow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var viewModel = new MarketViewModel(MarketTestSession.Create());
                var window = new DesktopShopWindow(viewModel);
                var requestCount = 0;
                window.OpenManagementRequested += (_, _) => requestCount++;

                window.SelectShopObject(new BusinessShopInteractionTarget(
                    BusinessShopInteractionKind.Shelf,
                    "ambient",
                    new LogicalPixelRect(0, 0, 1, 1)));

                Assert.Equal(ManagementSection.Overview, viewModel.SelectedSection);
                Assert.Equal("常温货架", viewModel.SelectedShopObject?.Title);
                Assert.Equal(1, requestCount);
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

    private static string FindDesktopShopWindowCodeBehindPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(
            directory.FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "Windows",
            "DesktopShopWindow.xaml.cs");
    }

    private static string FindDesktopShopWindowPath() =>
        Path.Combine(
            FindRepositoryRoot().FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "Windows",
            "DesktopShopWindow.xaml");

    private static string FindAppCodeBehindPath() =>
        Path.Combine(
            FindRepositoryRoot().FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "App.xaml.cs");

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }
}
