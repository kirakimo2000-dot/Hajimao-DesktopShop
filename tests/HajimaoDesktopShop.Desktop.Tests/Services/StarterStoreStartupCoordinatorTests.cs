using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class StarterStoreStartupCoordinatorTests
{
    [Fact]
    public void SelectForStartup_WithExistingSave_BypassesChoicePresenter()
    {
        var presenterCalls = 0;

        var result = StarterStoreStartupCoordinator.SelectForStartup(
            new GameSaveData(
                GameSaveSchema.CurrentVersion,
                DateTimeOffset.UtcNow,
                new ShopSaveData(0, 0, 0, 0, []),
                new SimulationSaveData(0, 0, 1, 0, [], [], null, [], null, null)),
            CreateCatalog(),
            seed: 42,
            _ =>
            {
                presenterCalls++;
                return false;
            });

        Assert.True(result.ShouldContinue);
        Assert.Null(result.Proposal);
        Assert.Equal(0, presenterCalls);
    }

    [Fact]
    public void SelectForStartup_NewGame_ReturnsThePresentedSelection()
    {
        var result = StarterStoreStartupCoordinator.SelectForStartup(
            save: null,
            CreateCatalog(),
            seed: 42,
            viewModel =>
            {
                viewModel.Choices.Single(choice => choice.FormatName == "平价量贩")
                    .SelectCommand.Execute(null);
                return true;
            });

        Assert.True(result.ShouldContinue);
        Assert.Equal("discount", result.Proposal?.FormatId);
    }

    [Fact]
    public void SelectForStartup_NewGameClosedWithoutSelection_CancelsStartup()
    {
        var result = StarterStoreStartupCoordinator.SelectForStartup(
            save: null,
            CreateCatalog(),
            seed: 42,
            _ => false);

        Assert.False(result.ShouldContinue);
        Assert.Null(result.Proposal);
    }

    private static StoreContentCatalog CreateCatalog() =>
        new(
        [
            Format("convenience", "社区便利"),
            Format("discount", "平价量贩"),
            Format("premium", "精品食品")
        ],
        [
            Brand("seven-eleven", "7-Eleven", "convenience"),
            Brand("aldi", "ALDI", "discount"),
            Brand("ginza-mitsukoshi", "银座三越", "premium")
        ]);

    private static StoreFormatDefinition Format(string id, string name) =>
        new(
            id, name, 40_000, 40_000, 1_000, 1_000, 1_000, 1_000, 1_000, 1_000,
            "steady",
            new Dictionary<string, int>
            {
                ["ambient"] = 1_000,
                ["chilled"] = 1_000,
                ["frozen"] = 1_000
            },
            StorePricingPreset.Balanced,
            StoreStockingPreset.Balanced);

    private static StoreBrandDefinition Brand(string id, string name, string formatId) =>
        new(id, name, "global", formatId, "facade", "real-world-name", "review-required");
}
