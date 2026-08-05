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

    [Fact]
    public void SelectShopObject_ShowsAccessibleDetailCardInMatchingSection()
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

                Assert.Equal(ManagementSection.Employees, viewModel.SelectedSection);
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

    [Fact]
    public void SelectedObjectCard_ExposesCommandsForTheCurrentObjectKind()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create(openingCashCents: 2_000_000));
            var window = new ManagementWindow(viewModel);
            try
            {
                window.SelectShopObject(new BusinessShopInteractionTarget(
                    BusinessShopInteractionKind.Shelf,
                    "ambient",
                    new LogicalPixelRect(0, 0, 1, 1)));
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                var quickRestock = Assert.IsType<Button>(window.FindName("SelectedShelfQuickRestock"));
                var autoRestock = Assert.IsType<Button>(window.FindName("SelectedShelfAutoRestock"));
                var train = Assert.IsType<Button>(window.FindName("SelectedEmployeeTrain"));
                var dayShift = Assert.IsType<Button>(window.FindName("SelectedEmployeeDayShift"));
                var nightShift = Assert.IsType<Button>(window.FindName("SelectedEmployeeNightShift"));
                UpdateButtonBindings(quickRestock, autoRestock, train, dayShift, nightShift);

                Assert.True(viewModel.IsShelfObjectSelected);
                Assert.False(viewModel.IsEmployeeObjectSelected);
                AssertVisibilityBinding(quickRestock, nameof(MarketViewModel.IsShelfObjectSelected));
                AssertVisibilityBinding(autoRestock, nameof(MarketViewModel.IsShelfObjectSelected));
                AssertVisibilityBinding(train, nameof(MarketViewModel.IsEmployeeObjectSelected));
                AssertCommandBinding(quickRestock, nameof(MarketViewModel.QuickRestockSelectedShelfCommand));
                AssertCommandBinding(autoRestock, nameof(MarketViewModel.ToggleAutoRestockSelectedShelfCommand));
                Assert.Equal("为货架最紧缺商品快速进货", AutomationProperties.GetName(quickRestock));
                Assert.Equal("切换货架最紧缺商品自动补货", AutomationProperties.GetName(autoRestock));

                window.SelectShopObject(new BusinessShopInteractionTarget(
                    BusinessShopInteractionKind.Employee,
                    "starter-cashier",
                    new LogicalPixelRect(0, 0, 1, 1)));
                UpdateButtonBindings(quickRestock, autoRestock, train, dayShift, nightShift);
                window.UpdateLayout();

                Assert.False(viewModel.IsShelfObjectSelected);
                Assert.True(viewModel.IsEmployeeObjectSelected);
                AssertVisibilityBinding(dayShift, nameof(MarketViewModel.IsEmployeeObjectSelected));
                AssertVisibilityBinding(nightShift, nameof(MarketViewModel.IsEmployeeObjectSelected));
                AssertCommandBinding(train, nameof(MarketViewModel.TrainSelectedEmployeeCommand));
                AssertCommandBinding(dayShift, nameof(MarketViewModel.SetSelectedEmployeeDayShiftCommand));
                AssertCommandBinding(nightShift, nameof(MarketViewModel.SetSelectedEmployeeNightShiftCommand));
                Assert.Equal("培训当前员工", AutomationProperties.GetName(train));
                Assert.Equal("将当前员工设为白班", AutomationProperties.GetName(dayShift));
                Assert.Equal("将当前员工设为夜班", AutomationProperties.GetName(nightShift));
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

    private static void UpdateButtonBindings(params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.GetBindingExpression(UIElement.VisibilityProperty)?.UpdateTarget();
            button.GetBindingExpression(Button.CommandProperty)?.UpdateTarget();
            button.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget();
        }
    }

    private static void AssertVisibilityBinding(Button button, string expectedPath)
    {
        var binding = BindingOperations.GetBindingExpression(button, UIElement.VisibilityProperty);
        Assert.NotNull(binding);
        Assert.Equal(expectedPath, binding.ParentBinding.Path.Path);
    }

    private static void AssertCommandBinding(Button button, string expectedPath)
    {
        var binding = BindingOperations.GetBindingExpression(button, Button.CommandProperty);
        Assert.NotNull(binding);
        Assert.Equal(expectedPath, binding.ParentBinding.Path.Path);
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
