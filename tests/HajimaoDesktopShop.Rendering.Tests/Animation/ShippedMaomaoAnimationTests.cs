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
        using var partsImage = SKImage.FromBitmap(parts);

        foreach (var clip in catalog.Clips.Values)
        {
            for (var frame = 0; frame < 24; frame++)
            {
                using var target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
                target.Erase(SKColors.Transparent);
                using var canvas = new SKCanvas(target);
                var pose = SkeletalAnimator.Evaluate(rig, clip, frame, reduceMotion: false);

                SkeletalCharacterRenderer.Draw(canvas, partsImage, rig, skin, pose, 32, 54);

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

    [Fact]
    public async Task CustomerWalk_UsesOpposedArmAndLegMotionAcrossAllTwentyFourFrames()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
        var content = await new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "humanoid.json"),
            Path.Combine(testData, "humanoid-clips.json"),
            Path.Combine(testData, "skins.json")).LoadAsync();
        var catalog = SkeletalAnimationCatalog.Create(content);
        var rig = catalog.Rigs["humanoid-v1"];
        var clip = catalog.Clips["customer-walk"];

        var first = SkeletalAnimator.Evaluate(rig, clip, 0, reduceMotion: false);
        var opposite = SkeletalAnimator.Evaluate(rig, clip, 12, reduceMotion: false);

        Assert.True(first.Bones["upper-arm-left"].RotationDegrees > 0);
        Assert.True(first.Bones["upper-arm-right"].RotationDegrees < 0);
        Assert.True(opposite.Bones["upper-arm-left"].RotationDegrees < 0);
        Assert.True(opposite.Bones["upper-arm-right"].RotationDegrees > 0);
        Assert.NotEqual(first.Bones["upper-leg-left"], opposite.Bones["upper-leg-left"]);
    }

    [Fact]
    public async Task IdlePose_KeepsTheTorsoAndPelvisVisuallyConnected()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
        var content = await new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "humanoid.json"),
            Path.Combine(testData, "humanoid-clips.json"),
            Path.Combine(testData, "skins.json")).LoadAsync();
        var catalog = SkeletalAnimationCatalog.Create(content);
        using var parts = SKBitmap.Decode(Path.Combine(testData, "maomao-parts.png"));
        using var partsImage = SKImage.FromBitmap(parts);
        using var target = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
        target.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(target);
        var pose = SkeletalAnimator.Evaluate(
            catalog.Rigs["humanoid-v1"],
            catalog.Clips["maomao-idle"],
            0,
            reduceMotion: false);

        SkeletalCharacterRenderer.Draw(
            canvas,
            partsImage,
            catalog.Rigs["humanoid-v1"],
            catalog.Skins["maomao-default"],
            pose,
            32,
            54);

        var longestEmptyRun = 0;
        var currentEmptyRun = 0;
        for (var y = 24; y <= 46; y++)
        {
            var hasCenterPixel = Enumerable.Range(27, 11)
                .Any(x => target.GetPixel(x, y).Alpha > 0);
            currentEmptyRun = hasCenterPixel ? 0 : currentEmptyRun + 1;
            longestEmptyRun = Math.Max(longestEmptyRun, currentEmptyRun);
        }

        Assert.InRange(longestEmptyRun, 0, 2);
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
