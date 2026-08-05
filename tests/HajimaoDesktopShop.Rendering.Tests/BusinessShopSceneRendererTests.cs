using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Domain.Streets;
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
    public void Renderer_MovesActorsAfterACompleteEightFrameAnimationCycle()
    {
        var origin = RenderFrame(animationFrame: 0, reduceMotion: false);
        var advanced = RenderFrame(animationFrame: 8, reduceMotion: false);

        Assert.NotEqual(origin, advanced);
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

    [Fact]
    public void Renderer_ShowsNoJourneyActorBeforeTheFirstVisitor()
    {
        var empty = RenderFrame(
            animationFrame: 0,
            reduceMotion: false,
            queueLength: 0,
            visitors: 0);
        var active = RenderFrame(
            animationFrame: 0,
            reduceMotion: false,
            queueLength: 0,
            visitors: 1);

        Assert.NotEqual(empty, active);
    }

    [Fact]
    public void Renderer_MovesJourneyActorAcrossCustomerStages()
    {
        var entering = RenderFrame(
            animationFrame: 0,
            reduceMotion: false,
            queueLength: 0,
            visitors: 1);
        var shelf = RenderFrame(
            animationFrame: 40,
            reduceMotion: false,
            queueLength: 0,
            visitors: 1);
        var leaving = RenderFrame(
            animationFrame: 88,
            reduceMotion: false,
            queueLength: 0,
            visitors: 1);

        Assert.NotEqual(entering, shelf);
        Assert.NotEqual(shelf, leaving);
    }

    private static string RenderFrame(
        int animationFrame,
        bool reduceMotion,
        int queueLength = 3,
        int visitors = 3)
    {
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new BusinessShopSceneRenderer();
        var frame = CreateFrame(queueLength, visitors) with
        {
            AnimationFrame = animationFrame,
            ReduceMotion = reduceMotion
        };

        renderer.Render(canvas, bitmap.Info, frame);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }

    private static BusinessShopSceneFrame CreateFrame(int queueLength, int visitors = 3)
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
            visitors,
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
            new EmployeeOperationsSnapshot(1, 1, [], []),
            new CommercialStreetSnapshot(
                CommercialStreetTier.Corner,
                StreetWeather.Clear,
                10_000,
                5,
                0,
                [new CommercialStreetStoreSnapshot("corner-store", "街角便利店", 10_000, 10_000)]));
        return new BusinessShopSceneFrame(snapshot, "corner-store");
    }
}
