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
    public void NavigationHighlight_FollowsSelectedSection()
    {
        var xaml = File.ReadAllText(FindManagementWindowPath());
        Assert.Contains("x:Name=\"OverviewSelectionIndicator\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StrategySelectionIndicator\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"InvestmentSelectionIndicator\"", xaml, StringComparison.Ordinal);
        Assert.Contains(
            "Visibility=\"{Binding IsOverviewSection, Converter={StaticResource BooleanToVisibilityConverter}}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "Visibility=\"{Binding IsStrategySection, Converter={StaticResource BooleanToVisibilityConverter}}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "Visibility=\"{Binding IsInvestmentSection, Converter={StaticResource BooleanToVisibilityConverter}}\"",
            xaml,
            StringComparison.Ordinal);

        var viewModel = new MarketViewModel(MarketTestSession.Create());
        Assert.True(viewModel.IsOverviewSection);
        Assert.False(viewModel.IsStrategySection);

        viewModel.NavigateCommand.Execute(ManagementSection.Strategy);

        Assert.False(viewModel.IsOverviewSection);
        Assert.True(viewModel.IsStrategySection);
    }

    [Fact]
    public void ManagementWindow_ContainsOneScrollableAccessibleNextActionRail()
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

                var scrollViewer = Assert.IsType<ScrollViewer>(window.FindName("RightRailScrollViewer"));
                Assert.Equal(ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
                Assert.Equal(ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);

                var panel = Assert.IsType<Border>(window.FindName("NextActionPanel"));
                Assert.Equal(Visibility.Visible, panel.Visibility);

                var action = Assert.IsType<Button>(window.FindName("NextActionButton"));
                Assert.Equal("选策略", viewModel.NextAction.ActionText);
                Assert.Equal(
                    "NextAction.ActionText",
                    BindingOperations.GetBindingExpression(
                        action,
                        AutomationProperties.NameProperty)?.ParentBinding.Path.Path);
                Assert.Equal(
                    "NextAction.ActionText",
                    BindingOperations.GetBindingExpression(
                        action,
                        ContentControl.ContentProperty)?.ParentBinding.Path.Path);
                Assert.Equal(
                    "GoToNextActionCommand",
                    BindingOperations.GetBindingExpression(
                        action,
                        Button.CommandProperty)?.ParentBinding.Path.Path);

                var xaml = File.ReadAllText(FindManagementWindowPath());
                Assert.Contains("Text=\"{Binding NextAction.ContextText}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Text=\"{Binding NextAction.Title}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Text=\"{Binding NextAction.DetailText}\"", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("x:Name=\"OnboardingPanel\"", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("x:Name=\"LongTermGoalPanel\"", xaml, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ManagementWindow_HasOnlyTheRequiredDesktopControlAndNoStatusOrSpeedText()
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
                Assert.Equal(
                    3,
                    FindLogicalChildren<Button>(window)
                        .Count(button => Equals(button.Tag, "management-navigation")));
                var scene = Assert.IsType<BusinessShopSceneControl>(window.FindName("LiveScene"));
                Assert.Equal("实时像素店铺场景", AutomationProperties.GetName(scene));
                Assert.DoesNotContain(
                    FindLogicalChildren<Button>(window),
                    button => Equals(button.Tag, "status-toggle"));
                var desktopControl = Assert.Single(
                    FindLogicalChildren<Button>(window),
                    button => Equals(button.Tag, "desktop-control"));
                Assert.Equal("切换鼠标穿透", desktopControl.Content);
                Assert.Equal(
                    nameof(MarketViewModel.ToggleClickThroughCommand),
                    BindingOperations.GetBindingExpression(
                        desktopControl,
                        Button.CommandProperty)?.ParentBinding.Path.Path);
                Assert.DoesNotContain(
                    FindLogicalChildren<Button>(window),
                    button => button.Content?.ToString() is "2x" or "4x" or "暂停" or "动画" or "特效" or "倍速");
                var xaml = File.ReadAllText(FindManagementWindowPath());
                Assert.DoesNotContain("TimeModeText", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("Text=\"{Binding StatusMessage}\"", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("ToggleMuteCommand", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("SoundToggleText", xaml, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void InvestmentSection_UsesComparableUnifiedCandidateCards()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            viewModel.NavigateCommand.Execute(ManagementSection.Investment);
            var window = new ManagementWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                var list = Assert.IsType<ItemsControl>(window.FindName("InvestmentCandidateList"));
                var itemsBinding = BindingOperations.GetBindingExpression(
                    list,
                    ItemsControl.ItemsSourceProperty);
                Assert.Equal(
                    $"{nameof(MarketViewModel.Investment)}.{nameof(InvestmentPortfolioViewModel.Candidates)}",
                    itemsBinding?.ParentBinding.Path.Path);
                var xaml = File.ReadAllText(FindManagementWindowPath());
                Assert.Contains("Tag=\"investment-action\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Command=\"{Binding InvestCommand}\"", xaml, StringComparison.Ordinal);
                foreach (var property in new[]
                         {
                             nameof(InvestmentCandidateCardViewModel.ThesisText),
                             nameof(InvestmentCandidateCardViewModel.StoreContextText),
                             nameof(InvestmentCandidateCardViewModel.TitleText),
                             nameof(InvestmentCandidateCardViewModel.CostText),
                             nameof(InvestmentCandidateCardViewModel.ExpectedBenefitText),
                             nameof(InvestmentCandidateCardViewModel.PaybackText),
                             nameof(InvestmentCandidateCardViewModel.CashPressureText),
                             nameof(InvestmentCandidateCardViewModel.EstimateConditionText)
                         })
                {
                    Assert.Contains($"Text=\"{{Binding {property}}}\"", xaml, StringComparison.Ordinal);
                }

                Assert.IsType<Border>(window.FindName("LatestInvestmentPanel"));
                Assert.Contains(
                    "每次只比较稳住弱店、提高回报、扩张街区三种资本用途。",
                    xaml,
                    StringComparison.Ordinal);
                Assert.DoesNotContain("StoreGrowth.Upgrade", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("EmployeeManagement.Candidates", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("Overview.OpenStoreCommand", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("Content=\"投资开店\"", xaml, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void StrategySection_ShowsConditionalRecoveryAction()
    {
        RunOnSta(() =>
        {
            var session = MarketTestSession.Create();
            session.Simulation.AdvanceRealSeconds(1_440);
            var viewModel = new MarketViewModel(session);
            viewModel.NavigateCommand.Execute(ManagementSection.Strategy);
            var window = new ManagementWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                var recovery = Assert.IsType<Button>(window.FindName("ApplyRecoveryAction"));
                Assert.Equal("采用保守方案", recovery.Content);
                Assert.Equal(
                    $"{nameof(MarketViewModel.Strategy)}.{nameof(StoreStrategyViewModel.ApplyRecoveryCommand)}",
                    BindingOperations.GetBindingExpression(
                        recovery,
                        Button.CommandProperty)?.ParentBinding.Path.Path);
                var recoveryPanel = Assert.IsType<Border>(window.FindName("RecoveryPanel"));
                Assert.Equal(Visibility.Visible, recoveryPanel.Visibility);
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
