using System.Security.Cryptography;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Characters;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.Characters;

public sealed class HumanCharacterRendererTests
{
    [Fact]
    public async Task Draw_PreservesTheNormalHumanBaseInsteadOfTintingTheWholeBody()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
        var content = await new JsonCharacterAnimationCatalog(
            Path.Combine(testData, "humanoid.json"),
            Path.Combine(testData, "humanoid-clips.json"),
            Path.Combine(testData, "skins.json")).LoadAsync();
        var catalog = SkeletalAnimationCatalog.Create(content);
        using var parts = SKBitmap.Decode(Path.Combine(testData, "maomao-parts.png"));
        var rig = catalog.Rigs["humanoid-v1"];
        var skin = catalog.Skins["maomao-default"];
        var pose = SkeletalAnimator.Evaluate(rig, catalog.Clips["customer-walk"], 0, false);
        using var partsImage = SKImage.FromBitmap(parts);
        using var paint = new SKPaint { IsAntialias = false };
        var profile = new HumanCharacterVisualProfile(
            ScaleX: 1,
            ScaleY: 1,
            GroundOffsetY: 0,
            AccentColor: SKColor.Parse("#00FF00"),
            Accessory: HumanCharacterAccessory.None);
        using var expected = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var expectedCanvas = new SKCanvas(expected);
        SkeletalCharacterRenderer.Draw(expectedCanvas, partsImage, rig, skin, pose, 32, 54);
        using var actual = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var actualCanvas = new SKCanvas(actual);

        HumanCharacterRenderer.Draw(
            actualCanvas,
            partsImage,
            paint,
            rig,
            skin,
            pose,
            profile,
            32,
            54,
            facingLeft: false);

        Assert.Equal(Hash(expected), Hash(actual));
        Assert.DoesNotContain(
            typeof(HumanCharacterVisualProfile).GetProperties(),
            property => property.Name == "BodyColor");
    }

    private static string Hash(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }
}
