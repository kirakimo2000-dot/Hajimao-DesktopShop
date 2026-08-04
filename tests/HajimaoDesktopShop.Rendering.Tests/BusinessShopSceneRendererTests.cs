using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Rendering;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class BusinessShopSceneRendererTests
{
    [Fact]
    public void Renderer_DrawsAggregateStoreWithoutAntialiasing()
    {
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new BusinessShopSceneRenderer();

        renderer.Render(canvas, bitmap.Info, CreateFrame(queueLength: 3));

        Assert.Equal(SKColor.Parse("#17191D"), bitmap.GetPixel(0, 0));
        Assert.Equal(SKColor.Parse("#F1B844"), bitmap.GetPixel(292, 112));
    }

    private static BusinessShopSceneFrame CreateFrame(int queueLength)
    {
        var business = new BusinessSnapshot(
            1,
            0,
            100_000,
            [
                new BusinessStoreSnapshot(
                    "corner-store",
                    "街角便利店",
                    0,
                    0,
                    0,
                    [new ProductSnapshot("water", "矿泉水", 100, 200, 0, 20, "ambient")])
            ]);
        var operations = new StoreOperationsSnapshot(
            "corner-store",
            3,
            3,
            0,
            0,
            queueLength,
            1_000,
            1_000,
            0,
            new DemandBreakdown(10_000, 0, 0, 0, 0, 0, 10_000));
        var snapshot = new BusinessSimulationSnapshot(
            0,
            business,
            [operations],
            new EmployeeOperationsSnapshot(1, 1, [], []));
        return new BusinessShopSceneFrame(snapshot, "corner-store");
    }
}
