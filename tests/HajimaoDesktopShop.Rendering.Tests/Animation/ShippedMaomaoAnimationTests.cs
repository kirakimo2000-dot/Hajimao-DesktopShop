using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering.Animation;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.Animation;

public sealed class ShippedMaomaoAnimationTests
{
    [Fact]
    public async Task EveryShippedClip_RendersAllTwentyFourFramesInsideThePreviewCanvas()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
        var content = await new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "humanoid.json"),
            Path.Combine(testData, "humanoid-clips.json"),
            Path.Combine(testData, "skins.json")).LoadAsync();
        var catalog = SkeletalAnimationCatalog.Create(content);
        var rig = catalog.Rigs["humanoid-v1"];
        var skin = catalog.Skins["maomao-default"];
        using var parts = SKBitmap.Decode(Path.Combine(testData, "maomao-parts.png"));
        Assert.NotNull(parts);

        foreach (var clip in catalog.Clips.Values)
        {
            for (var frame = 0; frame < 24; frame++)
            {
                using var target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
                target.Erase(SKColors.Transparent);
                using var canvas = new SKCanvas(target);
                var pose = SkeletalAnimator.Evaluate(rig, clip, frame, reduceMotion: false);

                SkeletalCharacterRenderer.Draw(canvas, parts, rig, skin, pose, 32, 54);

                var bounds = VisibleBounds(target);
                Assert.NotNull(bounds);
                Assert.InRange(bounds.Value.Left, 0, 63);
                Assert.InRange(bounds.Value.Top, 0, 63);
                Assert.InRange(bounds.Value.Right, 1, 64);
                Assert.InRange(bounds.Value.Bottom, 1, 64);
            }
        }
    }

    [Fact]
    public async Task ThrowClip_ReleaseMarkerUsesTheMovingProductSocket()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
        var content = await new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "humanoid.json"),
            Path.Combine(testData, "humanoid-clips.json"),
            Path.Combine(testData, "skins.json")).LoadAsync();
        var catalog = SkeletalAnimationCatalog.Create(content);
        var rig = catalog.Rigs["humanoid-v1"];
        var clip = catalog.Clips["maomao-throw"];

        var start = SkeletalAnimator.Evaluate(rig, clip, 0, reduceMotion: false);
        var release = SkeletalAnimator.Evaluate(rig, clip, 8, reduceMotion: false);

        Assert.Contains(release.Markers, marker => marker.Id == "release_product");
        Assert.NotEqual(
            start.Sockets["product_socket"],
            release.Sockets["product_socket"]);
    }

    private static SKRectI? VisibleBounds(SKBitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = 0;
        var bottom = 0;
        var found = false;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha == 0)
                {
                    continue;
                }

                found = true;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
            }
        }

        return found ? new SKRectI(left, top, right, bottom) : null;
    }
}
