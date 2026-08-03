using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Desktop.Windows;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class DesktopShopWindowRenderingTests
{
    [Fact]
    public void Content_UsesOneFlattenedSurfaceAndThreeAccessibleHitTargets()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var game = new ShopGameService(
                    [new ProductDefinition("water", "矿泉水", 100, 180, 20, "ambient")],
                    openingCashCents: 50_000);
                var simulation = new ShopSimulation(game, new NoSpawnRandomSource(), customerSpawnChance: 0d);
                var window = new DesktopShopWindow(new GameViewModel(game, simulation));
                var root = Assert.IsType<Grid>(window.Content);

                var surface = Assert.IsType<DesktopShopSurfaceControl>(root.Children[0]);
                Assert.True(surface.UsesLogicalPixelScaling);
                var hitTargets = Assert.IsType<Canvas>(root.Children[1]);
                Assert.Equal(3, hitTargets.Children.Count);

                window.Close();
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

    [Fact]
    public void DesktopAndManagementWindows_DoNotCreateTaskbarButtons()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var game = new ShopGameService(
                    [new ProductDefinition("water", "矿泉水", 100, 180, 20, "ambient")],
                    openingCashCents: 50_000);
                var simulation = new ShopSimulation(game, new NoSpawnRandomSource(), customerSpawnChance: 0d);
                var viewModel = new GameViewModel(game, simulation);
                var desktop = new DesktopShopWindow(viewModel);
                var management = new ManagementWindow(viewModel);

                Assert.False(desktop.ShowInTaskbar);
                Assert.False(management.ShowInTaskbar);

                management.Close();
                desktop.Close();
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

    private sealed class NoSpawnRandomSource : IRandomSource
    {
        public double NextDouble() => 1d;

        public int Next(int exclusiveMax) => 0;
    }
}
