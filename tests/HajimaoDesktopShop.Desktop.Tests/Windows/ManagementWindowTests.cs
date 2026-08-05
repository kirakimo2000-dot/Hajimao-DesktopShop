using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class ManagementWindowTests
{
    [Fact]
    public void ManagementWindow_ContainsAccessibleOnboardingPanel()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var window = new ManagementWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                Assert.False(window.ShowInTaskbar);
                Assert.Equal(ProductIdentity.ManagementWindowTitle, window.Title);
                Assert.Contains(
                    FindLogicalChildren<TextBlock>(window),
                    textBlock => textBlock.Text == ProductIdentity.BrandHeader);

                var panel = Assert.IsType<Border>(window.FindName("OnboardingPanel"));
                Assert.Equal(Visibility.Visible, panel.Visibility);
                var visibilityBinding = BindingOperations.GetBindingExpression(panel, UIElement.VisibilityProperty);
                Assert.NotNull(visibilityBinding);
                Assert.Equal(
                    $"{nameof(FrameworkElement.DataContext)}.{nameof(MarketViewModel.Onboarding)}.{nameof(OnboardingViewModel.IsVisible)}",
                    visibilityBinding.ParentBinding.Path.Path);

                var action = Assert.IsType<Button>(window.FindName("OnboardingAction"));
                Assert.Equal("前往当前新手任务", AutomationProperties.GetName(action));
                Assert.Equal("前往", action.Content);
                Assert.Same(viewModel.GoToOnboardingTaskCommand, action.Command);

                Assert.Contains(
                    FindLogicalChildren<TextBlock>(panel),
                    textBlock => BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty)?.ParentBinding.Path.Path
                        == $"{nameof(FrameworkElement.DataContext)}.{nameof(MarketViewModel.Onboarding)}.{nameof(OnboardingViewModel.ProgressText)}");
                Assert.Contains(
                    FindLogicalChildren<TextBlock>(panel),
                    textBlock => BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty)?.ParentBinding.Path.Path
                        == $"{nameof(FrameworkElement.DataContext)}.{nameof(MarketViewModel.Onboarding)}.{nameof(OnboardingViewModel.Title)}");
                Assert.Contains(
                    FindLogicalChildren<TextBlock>(panel),
                    textBlock => textBlock.TextWrapping == TextWrapping.Wrap
                        && BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty)?.ParentBinding.Path.Path
                            == $"{nameof(FrameworkElement.DataContext)}.{nameof(MarketViewModel.Onboarding)}.{nameof(OnboardingViewModel.Guidance)}");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ManagementWindow_HasSevenNavigationTargetsPersistentSceneAndNoSpeedControls()
    {
        RunOnSta(() =>
        {
            var window = new ManagementWindow(new MarketViewModel(MarketTestSession.Create()));
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                Assert.False(window.ShowInTaskbar);
                Assert.Equal(
                    7,
                    FindLogicalChildren<Button>(window)
                        .Count(button => Equals(button.Tag, "management-navigation")));
                var scene = Assert.IsType<BusinessShopSceneControl>(window.FindName("LiveScene"));
                Assert.Equal("实时像素店铺场景", AutomationProperties.GetName(scene));
                var street = Assert.IsType<CommercialStreetSceneControl>(window.FindName("StreetScene"));
                Assert.Equal("共享客流像素商业街", AutomationProperties.GetName(street));
                Assert.Single(
                    FindLogicalChildren<Button>(window),
                    button => Equals(button.Tag, "status-toggle"));
                Assert.DoesNotContain(
                    FindLogicalChildren<Button>(window),
                    button => button.Content?.ToString() is "2x" or "4x" or "暂停" or "动画" or "特效" or "倍速");
            }
            finally
            {
                window.Close();
            }
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "WPF verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
