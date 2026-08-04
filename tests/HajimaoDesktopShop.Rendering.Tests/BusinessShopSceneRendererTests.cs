using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Rendering;
using SkiaSharp;
using System.Security.Cryptography;

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
        Assert.NotEqual(SKColor.Parse("#4A353C"), bitmap.GetPixel(94, 70));
    }

    [Fact]
    public void Renderer_UsesDifferentAtlasFramesForAnimatedActors()
    {
        var seed = RenderFrame(animationFrame: 0, reduceMotion: false);
        var moving = RenderFrame(animationFrame: 2, reduceMotion: false);

        Assert.NotEqual(seed, moving);
    }

    [Fact]
    public void Renderer_ReducedMotionAlwaysUsesFrameZero()
    {
        var moving = RenderFrame(animationFrame: 3, reduceMotion: false);
        var reduced = RenderFrame(animationFrame: 3, reduceMotion: true);
        var seed = RenderFrame(animationFrame: 0, reduceMotion: false);

        Assert.NotEqual(seed, moving);
        Assert.Equal(seed, reduced);
    }

    [Fact]
    public void Renderer_CapsVisibleCustomersAtThePixelArtBudget()
    {
        var capped = RenderFrame(animationFrame: 0, reduceMotion: false, queueLength: 5);
        var overflow = RenderFrame(animationFrame: 0, reduceMotion: false, queueLength: 99);

        Assert.Equal(capped, overflow);
    }

    private static string RenderFrame(int animationFrame, bool reduceMotion, int queueLength = 3)
    {
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new BusinessShopSceneRenderer();
        var frame = CreateFrame(queueLength) with
        {
            AnimationFrame = animationFrame,
            ReduceMotion = reduceMotion
        };

        renderer.Render(canvas, bitmap.Info, frame);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
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
