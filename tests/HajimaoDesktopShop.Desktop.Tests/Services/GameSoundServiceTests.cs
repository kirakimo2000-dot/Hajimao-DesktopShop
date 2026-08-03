using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class GameSoundServiceTests
{
    [Fact]
    public void Feedback_PlaysMappedSoundUntilMuted()
    {
        var game = new ShopGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            openingCashCents: 10_000);
        var simulation = new ShopSimulation(game, new NoSpawnRandomSource(), customerSpawnChance: 0d);
        var viewModel = new GameViewModel(game, simulation);
        var output = new RecordingSoundOutput();
        using var service = new GameSoundService(viewModel, output);
        var water = Assert.Single(viewModel.Products);

        viewModel.QueueRestockCommand.Execute(water);
        viewModel.IncreasePriceCommand.Execute(water);
        viewModel.ToggleMuteCommand.Execute(null);
        viewModel.DecreasePriceCommand.Execute(water);

        Assert.Equal(
            [GameFeedbackKind.RestockQueued, GameFeedbackKind.PriceChanged],
            output.Played);
        Assert.True(viewModel.IsMuted);
        Assert.Equal("开启音效", viewModel.SoundToggleText);
    }

    private sealed class RecordingSoundOutput : IGameSoundOutput
    {
        public List<GameFeedbackKind> Played { get; } = [];

        public void Play(GameFeedbackKind kind) => Played.Add(kind);
    }

    private sealed class NoSpawnRandomSource : IRandomSource
    {
        public double NextDouble() => 1d;

        public int Next(int exclusiveMax) => 0;
    }
}
