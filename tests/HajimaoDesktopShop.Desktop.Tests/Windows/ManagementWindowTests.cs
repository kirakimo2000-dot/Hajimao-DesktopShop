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

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class ManagementWindowTests
{
    [Fact]
    public void ManagementWindow_AllowsCompactLogicalWorkAreasWithoutLosingScrollableContent()
    {
        RunOnSta(() =>
        {
            var window = new ManagementWindow(new MarketViewModel(MarketTestSession.Create()));
            try
            {
                Assert.True(window.MinWidth <= 840);
                Assert.True(window.MinHeight <= 540);

                window.ApplyTemplate();
                window.Measure(new Size(840, 540));
                window.Arrange(new Rect(0, 0, 840, 540));
                window.UpdateLayout();

                var content = Assert.IsType<Grid>(window.FindName("ManagementContent"));
                var rightRail = Assert.IsType<ScrollViewer>(window.FindName("RightRailScrollViewer"));
                Assert.True(content.MinWidth <= 360);
                Assert.True(rightRail.MinWidth <= 220);
                Assert.Equal(ScrollBarVisibility.Auto, rightRail.VerticalScrollBarVisibility);
            }
            finally
            {
                window.Close();
            }
        });
    }

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
    public void Overview_LeadsWithOneStoreDecisionSummaryInsteadOfAnAccountingGrid()
    {
        var xaml = File.ReadAllText(FindManagementWindowPath());

        Assert.Contains("Text=\"累计战果\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"当前状态\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"下一家店\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Economy.PerformanceHeadlineText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Economy.PerformanceDetailText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Economy.ReasonHeadlineText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Economy.ReasonDetailText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Investment.NextInvestmentTitle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Investment.NextInvestmentDetailText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding EventTicker.Text}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Investment.LatestComparisonText}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<UniformGrid Columns=\"3\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsSource=\"{Binding Overview.Stores}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"店铺组合\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagementWindow_UsesOneAccessiblePixelHierarchyWithoutAdHocColors()
    {
        var xaml = File.ReadAllText(FindManagementWindowPath());

        Assert.Equal(3, CountOccurrences(xaml, "Style=\"{DynamicResource NavigationPixelButton}\""));
        Assert.Contains("x:Name=\"TopMetricsPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ManagementContent\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{DynamicResource PageTitleText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{DynamicResource EyebrowText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"#", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Foreground=\"#", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("BorderBrush=\"#", xaml, StringComparison.Ordinal);
        Assert.Equal(0, CountOccurrences(xaml, "Style=\"{DynamicResource SelectedOptionButton}\""));
        Assert.Contains("ItemsSource=\"{Binding Loadout.Slots}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Collection.Products}\"", xaml, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"NextActionPanel\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedObjectCard", xaml, StringComparison.Ordinal);
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
                Assert.Equal("查看战斗", viewModel.NextAction.ActionText);
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
    public void ManagementOverview_LeadsWithCombatIdleRewards()
    {
        var xaml = File.ReadAllText(FindManagementWindowPath());

        Assert.Contains("x:Name=\"IdleRewardPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IdleFeedback.SessionProfitText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IdleFeedback.SessionCustomerText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IdleFeedback.SessionDropText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IdleFeedback.EscapedCustomerText}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportProgress", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"回到挂机街区\"", xaml, StringComparison.Ordinal);
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
                var scene = Assert.IsType<CombatShopSceneControl>(window.FindName("LiveScene"));
                Assert.Equal("实时像素店铺场景", AutomationProperties.GetName(scene));
                Assert.DoesNotContain(
                    FindLogicalChildren<Button>(window),
                    button => Equals(button.Tag, "status-toggle"));
                var desktopControl = Assert.Single(
                    FindLogicalChildren<Button>(window),
                    button => Equals(button.Tag, "desktop-control"));
                Assert.Equal("鼠标穿透", desktopControl.Content);
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

                Assert.Contains(
                    "从候选店型中选择下一家店；利润方式、客流规模和风险各不相同。",
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
    public void StrategySection_OnlyExposesLoadoutAndCollectionDecisions()
    {
        RunOnSta(() =>
        {
            var session = MarketTestSession.Create();
            var viewModel = new MarketViewModel(session);
            viewModel.NavigateCommand.Execute(ManagementSection.Strategy);
            var window = new ManagementWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1180, 720));
                window.Arrange(new Rect(0, 0, 1180, 720));
                window.UpdateLayout();

                var loadout = Assert.IsType<ItemsControl>(window.FindName("StoreLoadoutSlots"));
                Assert.Equal(
                    $"{nameof(MarketViewModel.Loadout)}.{nameof(StoreLoadoutViewModel.Slots)}",
                    BindingOperations.GetBindingExpression(
                        loadout,
                        ItemsControl.ItemsSourceProperty)?.ParentBinding.Path.Path);
                var collection = Assert.IsType<ItemsControl>(window.FindName("ProductCollectionList"));
                Assert.Equal(
                    $"{nameof(MarketViewModel.Collection)}.{nameof(ProductCollectionViewModel.Products)}",
                    BindingOperations.GetBindingExpression(
                        collection,
                        ItemsControl.ItemsSourceProperty)?.ParentBinding.Path.Path);

                var xaml = File.ReadAllText(FindManagementWindowPath());
                Assert.Contains("Command=\"{Binding Loadout.UseRecommendedLoadoutCommand}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Command=\"{Binding DataContext.Collection.EquipProductCommand, RelativeSource={RelativeSource AncestorType=Window}}\"", xaml, StringComparison.Ordinal);
                foreach (var removedTerm in new[] { "当前定价", "当前备货", "补货", "货架", "缺货" })
                {
                    Assert.DoesNotContain(removedTerm, xaml, StringComparison.Ordinal);
                }
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

    [Fact]
    public void ManagementWindow_RemainsOperableAtItsMinimumDesktopSize()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var window = new ManagementWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(840, 540));
                window.Arrange(new Rect(0, 0, 840, 540));
                window.UpdateLayout();

                var navigation = FindLogicalChildren<Button>(window)
                    .Where(button => Equals(button.Tag, "management-navigation"))
                    .ToArray();
                Assert.Equal(3, navigation.Length);
                Assert.All(navigation, button => Assert.True(button.MinHeight >= 44));

                var primaryActions = FindLogicalChildren<Button>(window)
                    .Where(button => button.Name is "NextActionButton" or "ApplyRecoveryAction")
                    .ToArray();
                Assert.All(primaryActions, button => Assert.True(button.MinHeight >= 44));

                var content = Assert.IsType<Grid>(window.FindName("ManagementContent"));
                Assert.InRange(content.MinWidth, 320, 360);

                var rightRail = Assert.IsType<ScrollViewer>(window.FindName("RightRailScrollViewer"));
                Assert.InRange(rightRail.MinWidth, 200, 220);
                Assert.Equal(ScrollBarVisibility.Auto, rightRail.VerticalScrollBarVisibility);
                Assert.Equal(ScrollBarVisibility.Disabled, rightRail.HorizontalScrollBarVisibility);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ManagementWindow_RendersOffscreenWithTheSharedTheme()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var window = new ManagementWindow(viewModel);
            try
            {
                UiSnapshotRenderer.Render(window, 1180, 720, "management-overview.png");
                viewModel.NavigateCommand.Execute(ManagementSection.Strategy);
                UiSnapshotRenderer.Render(window, 1180, 720, "management-strategy.png");
                viewModel.NavigateCommand.Execute(ManagementSection.Investment);
                UiSnapshotRenderer.Render(window, 1180, 720, "management-investment.png");
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += search.Length;
        }

        return count;
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
