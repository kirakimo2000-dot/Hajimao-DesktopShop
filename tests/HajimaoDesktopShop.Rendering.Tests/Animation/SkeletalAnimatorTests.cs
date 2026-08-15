using HajimaoDesktopShop.Rendering.Animation;

namespace HajimaoDesktopShop.Rendering.Tests.Animation;

public sealed class SkeletalAnimatorTests
{
    [Fact]
    public void Rig_RejectsCyclesAndSocketsBoundToUnknownBones()
    {
        Assert.Throws<ArgumentException>(() => new SkeletalRig(
            "cyclic",
            [
                new RigBone("torso", "head", 0, 0, 0),
                new RigBone("head", "torso", 0, -4, 1)
            ],
            []));

        Assert.Throws<ArgumentException>(() => new SkeletalRig(
            "missing-socket-bone",
            [new RigBone("root", null, 0, 0, 0)],
            [new RigSocket("product_socket", "hand", 1, 0)]));
    }

    [Fact]
    public void Clip_RequiresExactlyTwentyFourLogicalFrames()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SkeletalAnimationClip(
            "invalid",
            23,
            new Dictionary<string, IReadOnlyList<BoneKeyframe>>(),
            []));
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(23, false, 23)]
    [InlineData(24, false, 0)]
    [InlineData(-1, false, 23)]
    [InlineData(17, true, 0)]
    public void LogicalFrame_WrapsAndHonorsReducedMotion(
        long presentationFrame,
        bool reduceMotion,
        int expected)
    {
        Assert.Equal(expected, SkeletalAnimator.LogicalFrame(presentationFrame, reduceMotion));
    }

    [Fact]
    public void Evaluate_InterpolatesHierarchyAndSnapsSocketToWholePixels()
    {
        var rig = new SkeletalRig(
            "humanoid-v1",
            [
                new RigBone("root", null, 10, 20, 0),
                new RigBone("hand", "root", 5, 0, 1)
            ],
            [new RigSocket("product_socket", "hand", 1.6f, 0.4f)]);
        var clip = new SkeletalAnimationClip(
            "throw",
            24,
            new Dictionary<string, IReadOnlyList<BoneKeyframe>>(StringComparer.Ordinal)
            {
                ["root"] =
                [
                    new BoneKeyframe(0, BoneTransform.Identity),
                    new BoneKeyframe(12, BoneTransform.Identity with { TranslationX = 8 })
                ]
            },
            [new AnimationMarker(6, "release_product")]);

        var pose = SkeletalAnimator.Evaluate(rig, clip, presentationFrame: 6, reduceMotion: false);

        Assert.Equal(14, pose.Bones["root"].WorldX);
        Assert.Equal(19, pose.Bones["hand"].WorldX);
        Assert.Equal(21, pose.Sockets["product_socket"].WorldX);
        Assert.Equal(20, pose.Sockets["product_socket"].WorldY);
        Assert.Contains(pose.Markers, marker => marker.Id == "release_product");
    }

    [Fact]
    public void Skin_MapsReplaceablePartsWithoutEnteringAnimatorState()
    {
        var maomao = new CharacterSkin(
            "maomao-default",
            "humanoid-v1",
            new Dictionary<string, SkinPart>(StringComparer.Ordinal)
            {
                ["head"] = new("maomao-parts", 0, 0, 12, 12, 6, 10)
            });
        var futureCharacter = maomao with { Id = "future-character" };

        Assert.Equal("humanoid-v1", maomao.RigId);
        Assert.Equal(maomao.RigId, futureCharacter.RigId);
        Assert.NotEqual(maomao.Id, futureCharacter.Id);
    }
}
