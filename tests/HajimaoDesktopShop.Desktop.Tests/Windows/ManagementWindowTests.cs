using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;

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
    public void ManagementWindow_HasThreeInvestorNavigationTargetsPersistentSceneAndNoSpeedControls()
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
                    3,
                    FindLogicalChildren<Button>(window)
                        .Count(button => Equals(button.Tag, "management-navigation")));
                var scene = Assert.IsType<BusinessShopSceneControl>(window.FindName("LiveScene"));
                Assert.Equal("实时像素店铺场景", AutomationProperties.GetName(scene));
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

    [Fact]
    public void SelectShopObject_ShowsAccessibleReadOnlyDetailCardInOverview()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var window = new ManagementWindow(viewModel);
            try
            {
                window.SelectShopObject(new BusinessShopInteractionTarget(
                    BusinessShopInteractionKind.Employee,
                    "starter-restocker",
                    new LogicalPixelRect(0, 0, 1, 1)));
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                Assert.Equal(ManagementSection.Overview, viewModel.SelectedSection);
                var card = Assert.IsType<Border>(window.FindName("SelectedObjectCard"));
                Assert.Equal(Visibility.Visible, card.Visibility);
                Assert.Equal("当前店铺对象详情", AutomationProperties.GetName(card));
                Assert.Equal("阿澄", viewModel.SelectedShopObject?.Title);
                Assert.Equal("补货员", viewModel.SelectedShopObject?.CategoryText);
                Assert.Contains(
                    FindLogicalChildren<TextBlock>(card),
                    textBlock => BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty)?.ParentBinding.Path.Path
                        == $"{nameof(MarketViewModel.SelectedShopObject)}.{nameof(ShopObjectDetailViewModel.Title)}");
                Assert.Contains(
                    FindLogicalChildren<TextBlock>(card),
                    textBlock => BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty)?.ParentBinding.Path.Path
                        == $"{nameof(MarketViewModel.SelectedShopObject)}.{nameof(ShopObjectDetailViewModel.CategoryText)}");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData("快速进货")]
    [InlineData("区域 ×6")]
    [InlineData("刷新候选人")]
    [InlineData("白班")]
    [InlineData("夜班")]
    [InlineData("培训")]
    [InlineData("自动补货")]
    public void ManagementWindow_DoesNotExposeRoutineMaintenance(string forbiddenText)
    {
        Assert.DoesNotContain(
            forbiddenText,
            File.ReadAllText(FindManagementWindowPath()),
            StringComparison.Ordinal);
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

    private static string FindManagementWindowPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(
            directory.FullName,
            "src",
            "HajimaoDesktopShop.Desktop",
            "Windows",
            "ManagementWindow.xaml");
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
