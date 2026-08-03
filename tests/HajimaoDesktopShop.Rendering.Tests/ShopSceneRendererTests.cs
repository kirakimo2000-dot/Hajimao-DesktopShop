using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Simulation.Employees;
using HajimaoDesktopShop.Rendering;
using SkiaSharp;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class ShopSceneRendererTests
{
    [Fact]
    public void Render_DrawsDeterministicLogicalSceneAndActors()
    {
        using var bitmap = new SKBitmap(ShopSceneRenderer.LogicalWidth, ShopSceneRenderer.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new ShopSceneRenderer();

        renderer.Render(canvas, bitmap.Info, CreateSnapshot());

        Assert.Equal(SKColor.Parse("#4A353C"), bitmap.GetPixel(10, 10));
        Assert.Equal(SKColor.Parse("#B87349"), bitmap.GetPixel(10, 130));
        Assert.Equal(SKColor.Parse("#D7A64C"), bitmap.GetPixel(80, 70));
        Assert.Equal(SKColor.Parse("#F1B844"), bitmap.GetPixel(356, 94));
        Assert.Equal(SKColor.Parse("#72C986"), bitmap.GetPixel(22, 134));
    }

    private static SimulationSnapshot CreateSnapshot() =>
        new(
            30,
            new ShopSnapshot(
                50_000,
                0,
                0,
                0,
                [new ProductSnapshot("water", "矿泉水", 100, 180, 2, 20, "ambient")]),
            [new CustomerSnapshot(1, CustomerState.Entering, null)],
            [
                new EmployeeSnapshot("cashier-1", "小葵", EmployeeRole.Cashier, EmployeeState.Working, "checkout:1"),
                new EmployeeSnapshot("restocker-1", "阿满", EmployeeRole.Restocker, EmployeeState.Idle, null)
            ],
            0,
            0,
            0,
            null);
}
