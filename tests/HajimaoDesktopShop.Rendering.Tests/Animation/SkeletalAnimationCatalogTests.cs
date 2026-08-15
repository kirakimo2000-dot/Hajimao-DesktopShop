using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Rendering.Animation;

namespace HajimaoDesktopShop.Rendering.Tests.Animation;

public sealed class SkeletalAnimationCatalogTests
{
    [Fact]
    public void Create_MapsContentDefinitionsIntoRuntimeRigClipAndSkin()
    {
        var content = new CharacterAnimationContent(
            [new CharacterRigDefinition(
                "humanoid-v1",
                [new CharacterBoneDefinition("root", null, 3, 4, 0)],
                [new CharacterSocketDefinition("product_socket", "root", 2, 0)])],
            [new CharacterAnimationClipDefinition(
                "maomao-throw",
                "humanoid-v1",
                24,
                new Dictionary<string, IReadOnlyList<CharacterBoneKeyframeDefinition>>
                {
                    ["root"] = [new(0), new(12, TranslationX: 6)]
                },
                [new(6, "release_product")])],
            [new CharacterSkinDefinition(
                "maomao-default",
                "humanoid-v1",
                "maomao/parts.png",
                new Dictionary<string, CharacterSkinPartDefinition>
                {
                    ["root"] = new(0, 0, 8, 8, 4, 8)
                })]);

        var catalog = SkeletalAnimationCatalog.Create(content);
        var pose = SkeletalAnimator.Evaluate(
            catalog.Rigs["humanoid-v1"],
            catalog.Clips["maomao-throw"],
            presentationFrame: 6,
            reduceMotion: false);

        Assert.Equal(6, pose.Bones["root"].WorldX);
        Assert.Equal(8, pose.Sockets["product_socket"].WorldX);
        Assert.Equal("humanoid-v1", catalog.Skins["maomao-default"].RigId);
    }
}
