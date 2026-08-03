using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels;

public sealed class GameViewModelTests
{
    [Fact]
    public void Refresh_MapsEconomyInventoryActorsAndWarnings()
    {
        var (game, simulation, viewModel) = CreateSubject();
        game.PurchaseStock("water", 5);
        simulation.AdvanceRealSecond();

        viewModel.Refresh();

        Assert.Equal("¥95.00", viewModel.CashText);
        Assert.Equal("第 1 天 00:01", viewModel.GameTimeText);
        Assert.Equal("顾客 1", viewModel.CustomerCountText);
        Assert.Equal("缺货/低库存 1", viewModel.StockWarningText);
        Assert.Equal(2, viewModel.Products.Count);
        Assert.Equal("库存充足", Assert.Single(viewModel.Products, product => product.Id == "water").StockStatusText);
        Assert.Equal("已缺货", Assert.Single(viewModel.Products, product => product.Id == "milk").StockStatusText);
        Assert.Single(viewModel.Customers);
        Assert.Equal(2, viewModel.Employees.Count);
    }

    [Fact]
    public void ProductCommands_ChangePriceAndQueueRestockThroughApplicationBoundary()
    {
        var (_, simulation, viewModel) = CreateSubject();
        viewModel.Refresh();
        var water = Assert.Single(viewModel.Products, product => product.Id == "water");

        viewModel.IncreasePriceCommand.Execute(water);
        viewModel.QueueRestockCommand.Execute(water);
        simulation.AdvanceRealSeconds(2);
        viewModel.Refresh();

        water = Assert.Single(viewModel.Products, product => product.Id == "water");
        Assert.Equal(210, water.SalePriceCents);
        Assert.Equal(5, water.Quantity);
        Assert.Contains("已排入补货", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowCommands_UpdatePresentationState()
    {
        var (_, _, viewModel) = CreateSubject();

        viewModel.ToggleLockCommand.Execute(null);
        viewModel.ToggleClickThroughCommand.Execute(null);

        Assert.True(viewModel.IsLocked);
        Assert.True(viewModel.IsClickThrough);
    }

    [Fact]
    public void RestoreDesktopState_RestoresLockButAlwaysDisablesClickThrough()
    {
        var (_, _, viewModel) = CreateSubject();
        viewModel.ToggleClickThroughCommand.Execute(null);

        viewModel.RestoreDesktopState(isLocked: true);

        Assert.True(viewModel.IsLocked);
        Assert.False(viewModel.IsClickThrough);
    }

    private static (ShopGameService Game, ShopSimulation Simulation, GameViewModel ViewModel) CreateSubject()
    {
        var game = new ShopGameService(
            [
                new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient"),
                new ProductDefinition("milk", "鲜牛奶", 300, 480, 12, "chilled")
            ],
            openingCashCents: 10_000);
        var simulation = new ShopSimulation(
            game,
            new AlwaysSpawnFirstRandomSource(),
            customerSpawnChance: 0.5d,
            maxCustomers: 1);
        return (game, simulation, new GameViewModel(game, simulation));
    }

    private sealed class AlwaysSpawnFirstRandomSource : IRandomSource
    {
        private bool _spawned;

        public double NextDouble()
        {
            if (_spawned)
            {
                return 1d;
            }

            _spawned = true;
            return 0d;
        }

        public int Next(int exclusiveMax) => 0;
    }
}
