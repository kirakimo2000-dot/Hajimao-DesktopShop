using System.Runtime.ExceptionServices;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Desktop.Windows;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class StarterStoreChoiceWindowTests
{
    [Fact]
    public void StarterCards_ShowAVisualStorefrontAndReturnProfileBeforeCopy()
    {
        var xaml = File.ReadAllText(FindWindowPath());

        Assert.Contains("x:Name=\"StorefrontPreview\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ReturnProfileText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DecisionPromptText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding BrandName}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Window_RendersThreeAccessibleChoicesWithoutTaskbarEntry()
    {
        RunOnSta(() =>
        {
            var viewModel = new StarterStoreChoiceViewModel(CreateProposals());
            var window = new StarterStoreChoiceWindow(viewModel);
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(900, 480));
                window.Arrange(new Rect(0, 0, 900, 480));
                window.UpdateLayout();

                Assert.False(window.ShowInTaskbar);
                Assert.Equal(ResizeMode.CanResize, window.ResizeMode);
                Assert.True(window.MinWidth <= 760);
                Assert.True(window.MinHeight <= 560);
                Assert.Null(viewModel.SelectedProposal);
                var choices = Assert.IsType<ItemsControl>(window.FindName("StarterStoreChoices"));
                Assert.Equal(
                    nameof(StarterStoreChoiceViewModel.Choices),
                    BindingOperations.GetBindingExpression(
                        choices,
                        ItemsControl.ItemsSourceProperty)?.ParentBinding.Path.Path);
                Assert.Equal(3, viewModel.Choices.Count);
                Assert.Equal(
                    ["选择 7-Eleven", "选择 ALDI", "选择 银座三越"],
                    viewModel.Choices.Select(choice => $"选择 {choice.BrandName}"));
                var xaml = File.ReadAllText(FindWindowPath());
                Assert.Contains("x:Name=\"StarterChoiceScrollViewer\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Style=\"{DynamicResource PageTitleText}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Style=\"{DynamicResource ChoiceCard}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Text=\"{Binding DecisionPromptText}\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Text=\"主要风险\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Text=\"适合你\"", xaml, StringComparison.Ordinal);
                Assert.Contains("Tag=\"starter-store-choice\"", xaml, StringComparison.Ordinal);
                Assert.Contains(
                    "AutomationProperties.Name=\"{Binding BrandName, StringFormat=选择 {0}}\"",
                    xaml,
                    StringComparison.Ordinal);
                Assert.DoesNotContain("Background=\"#", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("Foreground=\"#", xaml, StringComparison.Ordinal);
                Assert.DoesNotContain("BorderBrush=\"#", xaml, StringComparison.Ordinal);
                UiSnapshotRenderer.Render(window, 960, 600, "starter-store-choice.png");
            }
            finally
            {
                window.Close();
            }

            Assert.Null(viewModel.SelectedProposal);
        });
    }

    private static IReadOnlyList<StoreOpeningProposal> CreateProposals() =>
    [
        Proposal("seven-eleven", "7-Eleven", "convenience", "社区便利"),
        Proposal("aldi", "ALDI", "discount", "平价量贩"),
        Proposal("ginza-mitsukoshi", "银座三越", "premium", "精品食品")
    ];

    private static StoreOpeningProposal Proposal(
        string brandId,
        string brandName,
        string formatId,
        string formatName) =>
        new("store-0001", 1, brandId, brandName, formatId, formatName, 0, 40_000, 120_000, true);

    private static string FindWindowPath()
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
            "StarterStoreChoiceWindow.xaml");
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
