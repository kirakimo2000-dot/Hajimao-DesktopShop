using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Animation;

public static class SkeletalCharacterRenderer
{
    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    public static void Draw(
        SKCanvas canvas,
        SKImage partsImage,
        SkeletalRig rig,
        CharacterSkin skin,
        SkeletalPose pose,
        float originX,
        float originY,
        float scale = 1,
        bool facingLeft = false) =>
        Draw(canvas, partsImage, rig, skin, pose, originX, originY, scale, scale, facingLeft);

    public static void Draw(
        SKCanvas canvas,
        SKImage partsImage,
        SkeletalRig rig,
        CharacterSkin skin,
        SkeletalPose pose,
        float originX,
        float originY,
        float scaleX,
        float scaleY,
        bool facingLeft = false) =>
        DrawCore(
            canvas,
            partsImage,
            rig,
            skin,
            pose,
            originX,
            originY,
            scaleX,
            scaleY,
            facingLeft);

    private static void DrawCore(
        SKCanvas canvas,
        SKImage partsImage,
        SkeletalRig rig,
        CharacterSkin skin,
        SkeletalPose pose,
        float originX,
        float originY,
        float scaleX,
        float scaleY,
        bool facingLeft)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(partsImage);
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(skin);
        ArgumentNullException.ThrowIfNull(pose);
        if (!string.Equals(skin.RigId, rig.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Skin '{skin.Id}' targets rig '{skin.RigId}', not '{rig.Id}'.",
                nameof(skin));
        }

        if (scaleX <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scaleX));
        }

        if (scaleY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scaleY));
        }

        canvas.Save();
        canvas.Translate(originX, originY);
        canvas.Scale(facingLeft ? -scaleX : scaleX, scaleY);

        foreach (var bone in rig.Bones.OrderBy(item => item.ZIndex).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!skin.Parts.TryGetValue(bone.Id, out var part)
                || !pose.Bones.TryGetValue(bone.Id, out var evaluated))
            {
                continue;
            }

            ValidatePartBounds(partsImage, skin, bone.Id, part);
            var source = new SKRect(
                part.SourceX,
                part.SourceY,
                part.SourceX + part.Width,
                part.SourceY + part.Height);
            var destination = new SKRect(
                -part.PivotX,
                -part.PivotY,
                part.Width - part.PivotX,
                part.Height - part.PivotY);

            canvas.Save();
            canvas.Translate(evaluated.WorldX, evaluated.WorldY);
            canvas.RotateDegrees(evaluated.RotationDegrees);
            canvas.Scale(evaluated.ScaleX, evaluated.ScaleY);
            canvas.DrawImage(partsImage, source, destination, PixelSampling, paint: null);
            canvas.Restore();
        }

        canvas.Restore();
    }

    private static void ValidatePartBounds(
        SKImage image,
        CharacterSkin skin,
        string boneId,
        SkinPart part)
    {
        if (part.SourceX < 0
            || part.SourceY < 0
            || part.Width <= 0
            || part.Height <= 0
            || part.SourceX + part.Width > image.Width
            || part.SourceY + part.Height > image.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(part),
                $"Skin '{skin.Id}' part '{boneId}' falls outside the parts bitmap.");
        }
    }
}
