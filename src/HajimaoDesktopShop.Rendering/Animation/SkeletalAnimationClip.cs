namespace HajimaoDesktopShop.Rendering.Animation;

public sealed class SkeletalAnimationClip
{
    public const int RequiredLogicalFrameCount = 24;

    public SkeletalAnimationClip(
        string id,
        int logicalFrameCount,
        IReadOnlyDictionary<string, IReadOnlyList<BoneKeyframe>> boneTracks,
        IReadOnlyList<AnimationMarker> markers)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Animation clip ID is required.", nameof(id));
        }

        if (logicalFrameCount != RequiredLogicalFrameCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(logicalFrameCount),
                logicalFrameCount,
                "Every character clip must contain exactly 24 logical frames.");
        }

        ArgumentNullException.ThrowIfNull(boneTracks);
        ArgumentNullException.ThrowIfNull(markers);

        Id = id.Trim();
        LogicalFrameCount = logicalFrameCount;
        BoneTracks = CopyTracks(boneTracks, logicalFrameCount);
        Markers = CopyMarkers(markers, logicalFrameCount);
    }

    public string Id { get; }

    public int LogicalFrameCount { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<BoneKeyframe>> BoneTracks { get; }

    public IReadOnlyList<AnimationMarker> Markers { get; }

    private static IReadOnlyDictionary<string, IReadOnlyList<BoneKeyframe>> CopyTracks(
        IReadOnlyDictionary<string, IReadOnlyList<BoneKeyframe>> source,
        int frameCount)
    {
        var result = new Dictionary<string, IReadOnlyList<BoneKeyframe>>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value.Count == 0)
            {
                throw new ArgumentException("Bone tracks require an ID and at least one keyframe.", nameof(source));
            }

            var frames = pair.Value.OrderBy(keyframe => keyframe.Frame).ToArray();
            if (frames.Any(keyframe => keyframe.Frame < 0 || keyframe.Frame >= frameCount)
                || frames.Select(keyframe => keyframe.Frame).Distinct().Count() != frames.Length)
            {
                throw new ArgumentException("Bone keyframes must be unique and inside the clip.", nameof(source));
            }

            result.Add(pair.Key, Array.AsReadOnly(frames));
        }

        return result;
    }

    private static IReadOnlyList<AnimationMarker> CopyMarkers(
        IReadOnlyList<AnimationMarker> source,
        int frameCount)
    {
        if (source.Any(marker => marker.Frame < 0
            || marker.Frame >= frameCount
            || string.IsNullOrWhiteSpace(marker.Id)))
        {
            throw new ArgumentException("Animation markers require an ID and a valid frame.", nameof(source));
        }

        return Array.AsReadOnly(source.OrderBy(marker => marker.Frame).ToArray());
    }
}
