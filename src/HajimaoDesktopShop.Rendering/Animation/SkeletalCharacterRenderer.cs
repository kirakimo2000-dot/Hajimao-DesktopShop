using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Animation;

public static class SkeletalCharacterRenderer
{
    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    public static void Draw(
        SKCanvas canvas,
        SKBitmap partsBitmap,
        SkeletalRig rig,
        CharacterSkin skin,
        SkeletalPose pose,
        float originX,
        float originY,
        float scale = 1,
        bool facingLeft = false,
        SKColor? tint = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(partsBitmap);
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(skin);
        ArgumentNullException.ThrowIfNull(pose);
        if (!string.Equals(skin.RigId, rig.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Skin '{skin.Id}' targets rig '{skin.RigId}', not '{rig.Id}'.",
                nameof(skin));
        }

        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        using var image = SKImage.FromBitmap(partsBitmap);
        using var paint = new SKPaint { IsAntialias = false };
        if (tint is { } tintColor)
        {
            paint.ColorFilter = SKColorFilter.CreateBlendMode(tintColor, SKBlendMode.Modulate);
        }
        canvas.Save();
        canvas.Translate(originX, originY);
        canvas.Scale(facingLeft ? -scale : scale, scale);

        foreach (var bone in rig.Bones.OrderBy(item => item.ZIndex).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!skin.Parts.TryGetValue(bone.Id, out var part)
                || !pose.Bones.TryGetValue(bone.Id, out var evaluated))
            {
                continue;
            }

            ValidatePartBounds(partsBitmap, skin, bone.Id, part);
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
            canvas.DrawImage(image, source, destination, PixelSampling, paint);
            canvas.Restore();
        }

        canvas.Restore();
    }

    private static void ValidatePartBounds(
        SKBitmap bitmap,
        CharacterSkin skin,
        string boneId,
        SkinPart part)
    {
        if (part.SourceX < 0
            || part.SourceY < 0
            || part.Width <= 0
            || part.Height <= 0
            || part.SourceX + part.Width > bitmap.Width
            || part.SourceY + part.Height > bitmap.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(part),
                $"Skin '{skin.Id}' part '{boneId}' falls outside the parts bitmap.");
        }
    }
}
