using System.Security.Cryptography;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class CommercialStreetSceneRendererTests
{
    [Fact]
    public void Renderer_DrawsOpenedStorefrontVehicleAndRainWithoutMutatingSnapshot()
    {
        var snapshot = CreateSnapshot(StreetWeather.Rain, pedestrians: 3, vehicles: 1);
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(snapshot.Stores.Count),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new CommercialStreetSceneRenderer();

        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

        Assert.Equal(SKColor.Parse("#65B8C8"), bitmap.GetPixel(8, 8));
        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(15, 50));
        Assert.Equal(SKColor.Parse("#E15A5A"), bitmap.GetPixel(50, 165));
        Assert.Equal(3, snapshot.VisiblePedestrians);
        Assert.Equal(1, snapshot.VisibleVehicles);
    }

    [Fact]
    public void Renderer_UsesAnimatedPedestrianFramesAndHonorsReducedMotion()
    {
        var seed = RenderHash(animationFrame: 0, reduceMotion: false);
        var moving = RenderHash(animationFrame: 2, reduceMotion: false);
        var reduced = RenderHash(animationFrame: 2, reduceMotion: true);

        Assert.NotEqual(seed, moving);
        Assert.Equal(seed, reduced);
    }

    [Fact]
    public void Renderer_CapsVisibleStreetActorsToSceneBudget()
    {
        var capped = RenderHash(0, false, pedestrians: 6, vehicles: 2);
        var overflow = RenderHash(0, false, pedestrians: 99, vehicles: 99);

        Assert.Equal(capped, overflow);
    }

    [Fact]
    public void Renderer_AcceptsOpenedStoresBeyondTheOldTierSlotCap()
    {
        var stores = Enumerable.Range(0, 12)
            .Select(index => new CommercialStreetStoreSnapshot(
                $"store-{index}",
                $"店铺 {index}",
                8_000,
                10_000 / 12))
            .ToArray();
        var snapshot = new CommercialStreetSnapshot(
            CommercialStreetTier.Corner,
            StreetWeather.Clear,
            8_000,
            0,
            0,
            stores);
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(stores.Length),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new CommercialStreetSceneRenderer();

        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

        var last = CommercialStreetLayout.CreateStorefronts(stores)[^1].Bounds;
        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(last.X + 4, last.Y + 4));
    }

    [Fact]
    public void Renderer_CameraOffsetMovesContentInsideAClampedViewport()
    {
        var snapshot = CreateSnapshot(StreetWeather.Clear, pedestrians: 0, vehicles: 0);
        using var bitmap = new SKBitmap(248, CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new CommercialStreetSceneRenderer();

        renderer.Render(
            canvas,
            bitmap.Info,
            new CommercialStreetSceneFrame(snapshot, CameraOffset: 232));

        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(16, 32));
    }

    private static string RenderHash(
        int animationFrame,
        bool reduceMotion,
        int pedestrians = 3,
        int vehicles = 1)
    {
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(2),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var renderer = new CommercialStreetSceneRenderer();
        renderer.Render(
            canvas,
            bitmap.Info,
            new CommercialStreetSceneFrame(
                CreateSnapshot(StreetWeather.Clear, pedestrians, vehicles),
                animationFrame,
                reduceMotion));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }

    private static CommercialStreetSnapshot CreateSnapshot(
        StreetWeather weather,
        int pedestrians,
        int vehicles) =>
        new(
            CommercialStreetTier.Street,
            weather,
            8_000,
            pedestrians,
            vehicles,
            [
                new CommercialStreetStoreSnapshot("corner", "街角店", 8_000, 6_000),
                new CommercialStreetStoreSnapshot("station", "车站店", 5_000, 4_000)
            ]);
}
