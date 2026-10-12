using HajimaoDesktopShop.Rendering.Animation;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Characters;

public static class HumanCharacterRenderer
{
    public static void Draw(
        SKCanvas canvas,
        SKImage parts,
        SKPaint paint,
        SkeletalRig rig,
        CharacterSkin skin,
        SkeletalPose pose,
        HumanCharacterVisualProfile profile,
        float originX,
        float originY,
        bool facingLeft)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(paint);
        SkeletalCharacterRenderer.Draw(
            canvas,
            parts,
            rig,
            skin,
            pose,
            originX,
            originY + profile.GroundOffsetY,
            profile.ScaleX,
            profile.ScaleY,
            facingLeft);
        DrawAccessory(canvas, paint, pose, profile, originX, originY, facingLeft);
    }

    public static SKPoint HeadAnchor(
        SkeletalPose pose,
        HumanCharacterVisualProfile profile,
        float originX,
        float originY,
        bool facingLeft)
    {
        ArgumentNullException.ThrowIfNull(pose);
        ArgumentNullException.ThrowIfNull(profile);
        var head = pose.Bones["head"];
        var x = originX + head.WorldX * profile.ScaleX * (facingLeft ? -1 : 1);
        var y = originY + profile.GroundOffsetY + (head.WorldY - 15) * profile.ScaleY;
        return new SKPoint(x, y);
    }

    private static void DrawAccessory(
        SKCanvas canvas,
        SKPaint paint,
        SkeletalPose pose,
        HumanCharacterVisualProfile profile,
        float originX,
        float originY,
        bool facingLeft)
    {
        if (profile.Accessory == HumanCharacterAccessory.None)
        {
            return;
        }

        paint.Color = profile.AccentColor;
        var head = pose.Bones["head"];
        var torso = pose.Bones["torso"];
        var hand = pose.Bones["hand-right"];
        canvas.Save();
        canvas.Translate(originX, originY + profile.GroundOffsetY);
        canvas.Scale(facingLeft ? -profile.ScaleX : profile.ScaleX, profile.ScaleY);
        switch (profile.Accessory)
        {
            case HumanCharacterAccessory.Cap:
                canvas.DrawRect(head.WorldX - 7, head.WorldY - 14, 14, 3, paint);
                canvas.DrawRect(head.WorldX + 4, head.WorldY - 11, 5, 2, paint);
                break;
            case HumanCharacterAccessory.Backpack:
                canvas.DrawRect(torso.WorldX + 5, torso.WorldY - 7, 7, 13, paint);
                DrawDarkPixel(canvas, torso.WorldX + 7, torso.WorldY - 3, paint);
                break;
            case HumanCharacterAccessory.Briefcase:
                canvas.DrawRect(hand.WorldX - 5, hand.WorldY + 3, 10, 7, paint);
                canvas.DrawRect(hand.WorldX - 2, hand.WorldY + 1, 4, 2, paint);
                break;
            case HumanCharacterAccessory.Camera:
                canvas.DrawRect(torso.WorldX - 5, torso.WorldY - 3, 10, 7, paint);
                DrawDarkPixel(canvas, torso.WorldX - 1, torso.WorldY, paint);
                break;
            case HumanCharacterAccessory.Cane:
                canvas.DrawRect(hand.WorldX + 3, hand.WorldY + 2, 2, 20, paint);
                canvas.DrawRect(hand.WorldX, hand.WorldY + 1, 5, 2, paint);
                break;
            case HumanCharacterAccessory.Helmet:
                canvas.DrawRect(head.WorldX - 7, head.WorldY - 15, 14, 5, paint);
                canvas.DrawRect(head.WorldX - 8, head.WorldY - 11, 16, 2, paint);
                break;
            case HumanCharacterAccessory.Tote:
                canvas.DrawRect(hand.WorldX - 5, hand.WorldY + 4, 11, 10, paint);
                canvas.DrawRect(hand.WorldX - 3, hand.WorldY + 1, 7, 3, paint);
                break;
            case HumanCharacterAccessory.Scarf:
                canvas.DrawRect(torso.WorldX - 6, torso.WorldY - 10, 12, 3, paint);
                canvas.DrawRect(torso.WorldX + 3, torso.WorldY - 7, 3, 9, paint);
                break;
            case HumanCharacterAccessory.Hood:
                canvas.DrawRect(head.WorldX - 8, head.WorldY - 14, 16, 4, paint);
                canvas.DrawRect(head.WorldX - 9, head.WorldY - 10, 3, 11, paint);
                canvas.DrawRect(head.WorldX + 6, head.WorldY - 10, 3, 11, paint);
                break;
            case HumanCharacterAccessory.CollectorHat:
                canvas.DrawRect(head.WorldX - 9, head.WorldY - 16, 18, 3, paint);
                canvas.DrawRect(head.WorldX - 5, head.WorldY - 22, 10, 6, paint);
                break;
        }

        canvas.Restore();
    }

    private static void DrawDarkPixel(SKCanvas canvas, float x, float y, SKPaint paint)
    {
        var original = paint.Color;
        paint.Color = SKColor.Parse("#302F36");
        canvas.DrawRect(x, y, 3, 3, paint);
        paint.Color = original;
    }
}
