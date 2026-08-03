using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Rendering;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class DesktopShopRendererTests
{
    [Fact]
    public void Render_FlattensDesktopChromeSceneAndStatusIntoOnePixelSurface()
    {
        using var bitmap = new SKBitmap(DesktopShopRenderer.LogicalWidth, DesktopShopRenderer.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new DesktopShopRenderer();
        var frame = new DesktopShopFrame(
            CreateSnapshot(),
            "¥500.00",
            "第 1 天 00:00",
            "缺货/低库存 1",
            "顾客 0",
            IsLocked: false,
            IsClickThrough: false);

        renderer.Render(canvas, bitmap.Info, frame);

        Assert.Equal(SKColor.Parse("#23262C"), bitmap.GetPixel(180, 20));
        Assert.Equal(SKColor.Parse("#F1B844"), bitmap.GetPixel(380, 20));
        Assert.Equal(SKColor.Parse("#D7A64C"), bitmap.GetPixel(80, 120));
        Assert.Equal(SKColor.Parse("#2D323A"), bitmap.GetPixel(200, 260));
        Assert.Equal(SKColor.Parse("#E15A5A"), bitmap.GetPixel(8, 255));
    }

    private static SimulationSnapshot CreateSnapshot() =>
        new(
            0,
            new ShopSnapshot(
                50_000,
                0,
                0,
                0,
                [new ProductSnapshot("water", "矿泉水", 100, 180, 5, 20, "ambient")]),
            [],
            [],
            0,
            0,
            0,
            null);
}
