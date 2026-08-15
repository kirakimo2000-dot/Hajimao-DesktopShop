using HajimaoDesktopShop.Rendering.Animation;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.Animation;

public sealed class SkeletalCharacterRendererTests
{
    [Fact]
    public void Draw_RendersSkinPartsInRigSpaceWithTransparentPixelSampling()
    {
        using var parts = new SKBitmap(16, 8, SKColorType.Rgba8888, SKAlphaType.Premul);
        parts.Erase(SKColors.Transparent);
        Fill(parts, 0, 0, 4, 4, SKColors.Red);
        Fill(parts, 4, 0, 4, 4, SKColors.Blue);
        var rig = new SkeletalRig(
            "test-rig",
            [
                new RigBone("root", null, 8, 8, 0),
                new RigBone("hand", "root", 5, 0, 1)
            ],
            []);
        var clip = new SkeletalAnimationClip(
            "idle",
            24,
            new Dictionary<string, IReadOnlyList<BoneKeyframe>>
            {
                ["root"] = [new(0, BoneTransform.Identity)]
            },
            []);
        var skin = new CharacterSkin(
            "test-skin",
            rig.Id,
            new Dictionary<string, SkinPart>
            {
                ["root"] = new("parts", 0, 0, 4, 4, 2, 2),
                ["hand"] = new("parts", 4, 0, 4, 4, 2, 2)
            });
        var pose = SkeletalAnimator.Evaluate(rig, clip, 0, reduceMotion: false);
        using var target = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Premul);
        target.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(target);

        SkeletalCharacterRenderer.Draw(canvas, parts, rig, skin, pose, 0, 0);

        Assert.Equal(SKColors.Red, target.GetPixel(8, 8));
        Assert.Equal(SKColors.Blue, target.GetPixel(13, 8));
        Assert.Equal(0, target.GetPixel(0, 0).Alpha);
    }

    private static void Fill(
        SKBitmap bitmap,
        int x,
        int y,
        int width,
        int height,
        SKColor color)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
            {
                bitmap.SetPixel(column, row, color);
            }
        }
    }
}
