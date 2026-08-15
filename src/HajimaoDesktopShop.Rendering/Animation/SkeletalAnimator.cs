using System.Numerics;

namespace HajimaoDesktopShop.Rendering.Animation;

public sealed record EvaluatedBonePose(
    int WorldX,
    int WorldY,
    float RotationDegrees,
    float ScaleX,
    float ScaleY,
    int ZIndex)
{
    internal Matrix3x2 Matrix { get; init; }
}

public sealed record EvaluatedSocketPose(int WorldX, int WorldY);

public sealed record SkeletalPose(
    int LogicalFrame,
    IReadOnlyDictionary<string, EvaluatedBonePose> Bones,
    IReadOnlyDictionary<string, EvaluatedSocketPose> Sockets,
    IReadOnlyList<AnimationMarker> Markers);

public static class SkeletalAnimator
{
    public static int LogicalFrame(long presentationFrame, bool reduceMotion)
    {
        if (reduceMotion)
        {
            return 0;
        }

        return (int)((presentationFrame % SkeletalAnimationClip.RequiredLogicalFrameCount
            + SkeletalAnimationClip.RequiredLogicalFrameCount)
            % SkeletalAnimationClip.RequiredLogicalFrameCount);
    }

    public static SkeletalPose Evaluate(
        SkeletalRig rig,
        SkeletalAnimationClip clip,
        long presentationFrame,
        bool reduceMotion)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(clip);

        foreach (var trackId in clip.BoneTracks.Keys)
        {
            if (!rig.BoneMap.ContainsKey(trackId))
            {
                throw new ArgumentException(
                    $"Clip '{clip.Id}' contains a track for unknown bone '{trackId}'.",
                    nameof(clip));
            }
        }

        var frame = LogicalFrame(presentationFrame, reduceMotion);
        var evaluated = new Dictionary<string, EvaluatedBonePose>(StringComparer.Ordinal);

        EvaluatedBonePose EvaluateBone(string boneId)
        {
            if (evaluated.TryGetValue(boneId, out var existing))
            {
                return existing;
            }

            var bone = rig.BoneMap[boneId];
            var transform = clip.BoneTracks.TryGetValue(boneId, out var track)
                ? Interpolate(track, frame, clip.LogicalFrameCount)
                : BoneTransform.Identity;
            var radians = transform.RotationDegrees * (MathF.PI / 180f);
            var local = Matrix3x2.CreateScale(transform.ScaleX, transform.ScaleY)
                * Matrix3x2.CreateRotation(radians)
                * Matrix3x2.CreateTranslation(
                    bone.PivotX + transform.TranslationX,
                    bone.PivotY + transform.TranslationY);
            var world = bone.ParentId is string parentId
                ? local * EvaluateBone(parentId).Matrix
                : local;
            var pose = new EvaluatedBonePose(
                Snap(world.M31),
                Snap(world.M32),
                MathF.Atan2(world.M12, world.M11) * (180f / MathF.PI),
                MathF.Sqrt((world.M11 * world.M11) + (world.M12 * world.M12)),
                MathF.Sqrt((world.M21 * world.M21) + (world.M22 * world.M22)),
                bone.ZIndex)
            {
                Matrix = world
            };
            evaluated.Add(boneId, pose);
            return pose;
        }

        foreach (var bone in rig.Bones)
        {
            EvaluateBone(bone.Id);
        }

        var sockets = new Dictionary<string, EvaluatedSocketPose>(StringComparer.Ordinal);
        foreach (var socket in rig.Sockets)
        {
            var bone = evaluated[socket.BoneId];
            var point = Vector2.Transform(new Vector2(socket.OffsetX, socket.OffsetY), bone.Matrix);
            sockets.Add(socket.Id, new EvaluatedSocketPose(Snap(point.X), Snap(point.Y)));
        }

        var markers = clip.Markers.Where(marker => marker.Frame == frame).ToArray();
        return new SkeletalPose(
            frame,
            evaluated,
            sockets,
            Array.AsReadOnly(markers));
    }

    private static BoneTransform Interpolate(
        IReadOnlyList<BoneKeyframe> track,
        int frame,
        int frameCount)
    {
        if (track.Count == 1)
        {
            return track[0].Transform;
        }

        BoneKeyframe? previous = null;
        BoneKeyframe? next = null;
        foreach (var keyframe in track)
        {
            if (keyframe.Frame <= frame)
            {
                previous = keyframe;
            }
            else
            {
                next = keyframe;
                break;
            }
        }

        var start = previous ?? track[^1] with { Frame = track[^1].Frame - frameCount };
        var end = next ?? track[0] with { Frame = track[0].Frame + frameCount };
        var adjustedFrame = previous is null ? frame - frameCount : frame;
        var duration = end.Frame - start.Frame;
        var amount = duration == 0 ? 0 : (float)(adjustedFrame - start.Frame) / duration;
        return Lerp(start.Transform, end.Transform, amount);
    }

    private static BoneTransform Lerp(BoneTransform from, BoneTransform to, float amount) =>
        new(
            LerpValue(from.TranslationX, to.TranslationX, amount),
            LerpValue(from.TranslationY, to.TranslationY, amount),
            LerpValue(from.RotationDegrees, to.RotationDegrees, amount),
            LerpValue(from.ScaleX, to.ScaleX, amount),
            LerpValue(from.ScaleY, to.ScaleY, amount));

    private static float LerpValue(float from, float to, float amount) =>
        from + ((to - from) * amount);

    private static int Snap(float value) => (int)MathF.Round(value);
}
