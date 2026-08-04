using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class ManagementWindowTests
{
    [Fact]
    public void ManagementWindow_HasSevenNavigationTargetsPersistentSceneAndNoSpeedControls()
    {
        RunOnSta(() =>
        {
            var window = new ManagementWindow(new MarketViewModel(MarketTestSession.Create()));
            window.ApplyTemplate();
            window.Measure(new Size(1180, 720));
            window.Arrange(new Rect(0, 0, 1180, 720));
            window.UpdateLayout();

            Assert.False(window.ShowInTaskbar);
            Assert.Equal(
                7,
                FindLogicalChildren<Button>(window)
                    .Count(button => Equals(button.Tag, "management-navigation")));
            Assert.IsType<BusinessShopSceneControl>(window.FindName("LiveScene"));
            Assert.DoesNotContain(
                FindLogicalChildren<Button>(window),
                button => button.Content?.ToString() is "2x" or "4x" or "暂停");

            window.Close();
        });
    }

    private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindLogicalChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
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
