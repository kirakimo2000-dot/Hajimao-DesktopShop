using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Rendering.Animation;

public sealed record SkeletalAnimationCatalog(
    IReadOnlyDictionary<string, SkeletalRig> Rigs,
    IReadOnlyDictionary<string, SkeletalAnimationClip> Clips,
    IReadOnlyDictionary<string, CharacterSkin> Skins)
{
    public static SkeletalAnimationCatalog Create(CharacterAnimationContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var rigs = content.Rigs.ToDictionary(
            definition => definition.Id,
            definition => new SkeletalRig(
                definition.Id,
                definition.Bones
                    .Select(bone => new RigBone(
                        bone.Id,
                        bone.ParentId,
                        bone.PivotX,
                        bone.PivotY,
                        bone.ZIndex))
                    .ToArray(),
                definition.Sockets
                    .Select(socket => new RigSocket(
                        socket.Id,
                        socket.BoneId,
                        socket.OffsetX,
                        socket.OffsetY))
                    .ToArray()),
            StringComparer.Ordinal);

        var clips = content.Clips.ToDictionary(
            definition => definition.Id,
            definition =>
            {
                if (!rigs.ContainsKey(definition.RigId))
                {
                    throw new ArgumentException(
                        $"Clip '{definition.Id}' references unknown rig '{definition.RigId}'.",
                        nameof(content));
                }

                return new SkeletalAnimationClip(
                    definition.Id,
                    definition.LogicalFrameCount,
                    definition.BoneTracks.ToDictionary(
                        pair => pair.Key,
                        pair => (IReadOnlyList<BoneKeyframe>)Array.AsReadOnly(pair.Value
                            .Select(keyframe => new BoneKeyframe(
                                keyframe.Frame,
                                new BoneTransform(
                                    keyframe.TranslationX,
                                    keyframe.TranslationY,
                                    keyframe.RotationDegrees,
                                    keyframe.ScaleX,
                                    keyframe.ScaleY)))
                            .ToArray()),
                        StringComparer.Ordinal),
                    definition.Markers
                        .Select(marker => new AnimationMarker(marker.Frame, marker.Id))
                        .ToArray());
            },
            StringComparer.Ordinal);

        var skins = content.Skins.ToDictionary(
            definition => definition.Id,
            definition =>
            {
                if (!rigs.ContainsKey(definition.RigId))
                {
                    throw new ArgumentException(
                        $"Skin '{definition.Id}' references unknown rig '{definition.RigId}'.",
                        nameof(content));
                }

                return new CharacterSkin(
                    definition.Id,
                    definition.RigId,
                    definition.Parts.ToDictionary(
                        pair => pair.Key,
                        pair => new SkinPart(
                            definition.AssetPath,
                            pair.Value.SourceX,
                            pair.Value.SourceY,
                            pair.Value.Width,
                            pair.Value.Height,
                            pair.Value.PivotX,
                            pair.Value.PivotY),
                        StringComparer.Ordinal));
            },
            StringComparer.Ordinal);

        return new SkeletalAnimationCatalog(rigs, clips, skins);
    }
}
