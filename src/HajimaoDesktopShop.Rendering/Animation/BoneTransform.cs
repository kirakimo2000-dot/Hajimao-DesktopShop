namespace HajimaoDesktopShop.Rendering.Animation;

public readonly record struct BoneTransform(
    float TranslationX,
    float TranslationY,
    float RotationDegrees,
    float ScaleX,
    float ScaleY)
{
    public static BoneTransform Identity { get; } = new(0, 0, 0, 1, 1);
}

public sealed record BoneKeyframe(int Frame, BoneTransform Transform);

public sealed record AnimationMarker(int Frame, string Id);
