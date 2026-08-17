using System.Runtime.ExceptionServices;
using System.IO;
using System.Windows.Controls;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;
using HajimaoDesktopShop.Rendering;

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
        Assert.Contains("AdjustAfterDrag();", dragHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplySurfaceLayout", dragHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void DragAndNavigation_ConstrainTheResizedSurfaceToTheCurrentWorkArea()
    {
        var source = File.ReadAllText(FindDesktopShopWindowCodeBehindPath());

        Assert.Contains("private void AdjustAfterDrag()", source, StringComparison.Ordinal);
        Assert.Contains(
            "DesktopWindowPlacementPolicy.ConstrainToWorkArea",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain("private void SnapAboveTaskbarIfNear()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopSurface_RemainsTopmostWhileManagementIsOpen()
    {
        var xaml = File.ReadAllText(FindDesktopShopWindowPath());
        var appSource = File.ReadAllText(FindAppCodeBehindPath());

        Assert.Contains("Topmost=\"True\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("_desktopWindow.Topmost = false", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleMuteCommand", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopSurface_UsesOpaqueWindowCompositionSoPortableCannotBecomeInvisible()
    {
        var xaml = File.ReadAllText(FindDesktopShopWindowPath());

        Assert.Contains("AllowsTransparency=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains(
            "Background=\"{DynamicResource Brush.Background}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AllowsTransparency=\"True\" Background=\"Transparent\"",
            xaml,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopSurface_UsesSemanticCompactControlsAndReadableEventCopy()
    {
        var xaml = File.ReadAllText(FindDesktopShopWindowPath());

        Assert.Contains("x:Key=\"SurfaceHitTarget\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"44\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"MinWidth\" Value=\"44\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontSize=\"12\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"IdleFeedbackBar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IdleFeedback.SessionProfitShortText}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DayCountdown", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"#", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Foreground=\"#", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("BorderBrush=\"#", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void StreetSurface_HidesTasksAndPlacesSpecialEventsAtTheTop()
    {
        var xaml = File.ReadAllText(FindDesktopShopWindowPath());

        Assert.DoesNotContain(
            "Text=\"{Binding IdleFeedback.RecentActivityText}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Text=\"{Binding IdleFeedback.MilestoneProgressText}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Value=\"{Binding IdleFeedback.MilestoneProgressPercent, Mode=OneWay}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Text=\"{Binding IdleFeedback.GoalText}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StreetEventPopup\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Panel.ZIndex=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"特殊事件\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding EventTicker.Text}\"", xaml, StringComparison.Ordinal);
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
                Assert.Equal(3, root.ContextMenu?.Items.Count);
                Assert.Equal(248, window.Width);
                Assert.Equal(180, window.Height);

                var streetPage = Assert.IsType<Grid>(root.FindName("StreetPage"));
                var streetHost = Assert.IsType<Grid>(root.FindName("StreetSurfaceHost"));
                var street = Assert.IsType<CommercialStreetSceneControl>(
                    Assert.Single(streetHost.Children));
                Assert.True(street.UsesLogicalPixelScaling);
                Assert.Equal(System.Windows.Visibility.Visible, streetPage.Visibility);
                UiSnapshotRenderer.Render(window, 248, 180, "desktop-street.png");

                var storePage = Assert.IsType<Grid>(root.FindName("StorePage"));
                var storeHost = Assert.IsType<Grid>(root.FindName("StoreSurfaceHost"));
                Assert.Empty(storeHost.Children);
                Assert.Equal(System.Windows.Visibility.Collapsed, storePage.Visibility);

                viewModel.DesktopNavigation.OpenStoreCommand.Execute("corner-store");
                streetPage.GetBindingExpression(Grid.VisibilityProperty)?.UpdateTarget();
                storePage.GetBindingExpression(Grid.VisibilityProperty)?.UpdateTarget();

                Assert.Equal(420, window.Width);
                Assert.Equal(280, window.Height);
                Assert.Equal(System.Windows.Visibility.Collapsed, streetPage.Visibility);
                Assert.Equal(System.Windows.Visibility.Visible, storePage.Visibility);
                Assert.Empty(streetHost.Children);
                var surface = Assert.IsType<CombatDesktopShopSurfaceControl>(
                    Assert.Single(storeHost.Children));
                Assert.True(surface.UsesLogicalPixelScaling);
                UiSnapshotRenderer.Render(window, 420, 280, "desktop-store.png");

                viewModel.DesktopNavigation.BackToStreetCommand.Execute(null);
                Assert.Equal(248, window.Width);
                Assert.Equal(180, window.Height);
                Assert.Empty(storeHost.Children);
                Assert.IsType<CommercialStreetSceneControl>(Assert.Single(streetHost.Children));

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
