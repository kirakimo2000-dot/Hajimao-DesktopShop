using System.Text.Json;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Infrastructure.Configuration;

public sealed class JsonCharacterAnimationCatalog
{
    private const int SchemaVersion = 1;
    private const int LogicalFrameCount = 24;
    private static readonly string[] RequiredClipIds =
    [
        "maomao-idle",
        "maomao-walk",
        "maomao-wind-up",
        "maomao-throw",
        "maomao-recovery",
        "maomao-celebrate",
        "customer-walk",
        "customer-hit",
        "customer-served",
        "customer-leave"
    ];
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _rigsPath;
    private readonly string _clipsPath;
    private readonly string _skinsPath;

    public JsonCharacterAnimationCatalog(
        string rigsPath,
        string clipsPath,
        string skinsPath)
    {
        _rigsPath = NormalizePath(rigsPath, nameof(rigsPath));
        _clipsPath = NormalizePath(clipsPath, nameof(clipsPath));
        _skinsPath = NormalizePath(skinsPath, nameof(skinsPath));
    }

    public async Task<CharacterAnimationContent> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var rigsDocument = await LoadAsync<RigCatalogDocument>(_rigsPath, cancellationToken);
        var clipsDocument = await LoadAsync<ClipCatalogDocument>(_clipsPath, cancellationToken);
        var skinsDocument = await LoadAsync<SkinCatalogDocument>(_skinsPath, cancellationToken);
        ValidateSchema(rigsDocument.SchemaVersion, "rig");
        ValidateSchema(clipsDocument.SchemaVersion, "animation clip");
        ValidateSchema(skinsDocument.SchemaVersion, "skin");

        var rigs = rigsDocument.Rigs?.ToArray()
            ?? throw new InvalidDataException("Character rig catalog has no rigs.");
        var clips = clipsDocument.Clips?.ToArray()
            ?? throw new InvalidDataException("Character animation catalog has no clips.");
        var skins = skinsDocument.Skins?.ToArray()
            ?? throw new InvalidDataException("Character skin catalog has no skins.");

        ValidateUniqueIds(rigs, rig => rig.Id, "rig");
        ValidateUniqueIds(clips, clip => clip.Id, "animation clip");
        ValidateUniqueIds(skins, skin => skin.Id, "skin");
        ValidateRigs(rigs);
        ValidateClips(clips, rigs);
        ValidateSkins(skins, rigs);

        return new CharacterAnimationContent(
            Array.AsReadOnly(rigs),
            Array.AsReadOnly(clips),
            Array.AsReadOnly(skins));
    }

    private static async Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidDataException($"Content file '{Path.GetFileName(path)}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Content file '{Path.GetFileName(path)}' is invalid: {exception.Message}",
                exception);
        }
    }

    private static void ValidateSchema(int version, string kind)
    {
        if (version != SchemaVersion)
        {
            throw new InvalidDataException($"Unsupported {kind} catalog schema version: {version}.");
        }
    }

    private static void ValidateRigs(IReadOnlyList<CharacterRigDefinition> rigs)
    {
        if (rigs.Count == 0)
        {
            throw new InvalidDataException("Character rig catalog requires at least one rig.");
        }

        foreach (var rig in rigs)
        {
            if (string.IsNullOrWhiteSpace(rig.Id) || rig.Bones.Count == 0)
            {
                throw new InvalidDataException("Every character rig requires an ID and bones.");
            }

            ValidateUniqueIds(rig.Bones, bone => bone.Id, $"bone in rig '{rig.Id}'");
            ValidateUniqueIds(rig.Sockets, socket => socket.Id, $"socket in rig '{rig.Id}'");
            var bones = rig.Bones.ToDictionary(bone => bone.Id, StringComparer.Ordinal);
            foreach (var bone in rig.Bones)
            {
                if (bone.ParentId is not null && !bones.ContainsKey(bone.ParentId))
                {
                    throw new InvalidDataException(
                        $"Rig '{rig.Id}' bone '{bone.Id}' has unknown parent '{bone.ParentId}'.");
                }

                var visited = new HashSet<string>(StringComparer.Ordinal);
                var current = bone;
                while (current.ParentId is string parentId)
                {
                    if (!visited.Add(current.Id))
                    {
                        throw new InvalidDataException($"Rig '{rig.Id}' contains a bone cycle.");
                    }

                    current = bones[parentId];
                }
            }

            foreach (var socket in rig.Sockets)
            {
                if (!bones.ContainsKey(socket.BoneId))
                {
                    throw new InvalidDataException(
                        $"Rig '{rig.Id}' socket '{socket.Id}' has unknown bone '{socket.BoneId}'.");
                }
            }
        }
    }

    private static void ValidateClips(
        IReadOnlyList<CharacterAnimationClipDefinition> clips,
        IReadOnlyList<CharacterRigDefinition> rigs)
    {
        var rigMap = rigs.ToDictionary(rig => rig.Id, StringComparer.Ordinal);
        var clipIds = clips.Select(clip => clip.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var required in RequiredClipIds)
        {
            if (!clipIds.Contains(required))
            {
                throw new InvalidDataException($"Required animation clip '{required}' is missing.");
            }
        }

        foreach (var clip in clips)
        {
            if (!rigMap.TryGetValue(clip.RigId, out var rig))
            {
                throw new InvalidDataException(
                    $"Animation clip '{clip.Id}' has unknown rig '{clip.RigId}'.");
            }

            if (clip.LogicalFrameCount != LogicalFrameCount || clip.BoneTracks.Count == 0)
            {
                throw new InvalidDataException(
                    $"Animation clip '{clip.Id}' requires tracks and exactly 24 logical frames.");
            }

            var boneIds = rig.Bones.Select(bone => bone.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var track in clip.BoneTracks)
            {
                if (!boneIds.Contains(track.Key) || track.Value.Count == 0)
                {
                    throw new InvalidDataException(
                        $"Animation clip '{clip.Id}' contains an invalid bone track '{track.Key}'.");
                }

                var frames = track.Value.Select(keyframe => keyframe.Frame).ToArray();
                if (frames.Any(frame => frame < 0 || frame >= LogicalFrameCount)
                    || frames.Distinct().Count() != frames.Length
                    || track.Value.Any(keyframe => keyframe.ScaleX <= 0 || keyframe.ScaleY <= 0))
                {
                    throw new InvalidDataException(
                        $"Animation clip '{clip.Id}' contains invalid keyframes.");
                }
            }

            if (clip.Markers.Any(marker => marker.Frame < 0
                || marker.Frame >= LogicalFrameCount
                || string.IsNullOrWhiteSpace(marker.Id)))
            {
                throw new InvalidDataException($"Animation clip '{clip.Id}' contains invalid markers.");
            }
        }

        var throwClip = clips.Single(clip => clip.Id == "maomao-throw");
        if (!throwClip.Markers.Any(marker => marker.Id == "release_product"))
        {
            throw new InvalidDataException("Maomao throw clip requires a release_product marker.");
        }
    }

    private static void ValidateSkins(
        IReadOnlyList<CharacterSkinDefinition> skins,
        IReadOnlyList<CharacterRigDefinition> rigs)
    {
        var rigMap = rigs.ToDictionary(rig => rig.Id, StringComparer.Ordinal);
        if (skins.Count(skin => skin.Id == "maomao-default") != 1)
        {
            throw new InvalidDataException("Character catalog requires exactly one maomao-default skin.");
        }

        foreach (var skin in skins)
        {
            if (!rigMap.TryGetValue(skin.RigId, out var rig)
                || string.IsNullOrWhiteSpace(skin.AssetPath)
                || skin.Parts.Count == 0)
            {
                throw new InvalidDataException($"Character skin '{skin.Id}' is incomplete.");
            }

            var boneIds = rig.Bones.Select(bone => bone.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var part in skin.Parts)
            {
                if (!boneIds.Contains(part.Key)
                    || part.Value.Width <= 0
                    || part.Value.Height <= 0
                    || part.Value.SourceX < 0
                    || part.Value.SourceY < 0)
                {
                    throw new InvalidDataException(
                        $"Character skin '{skin.Id}' has invalid part '{part.Key}'.");
                }
            }
        }
    }

    private static void ValidateUniqueIds<T>(
        IEnumerable<T> items,
        Func<T, string> idSelector,
        string kind)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            {
                throw new InvalidDataException($"Duplicate or empty {kind} ID: '{id}'.");
            }
        }
    }

    private static string NormalizePath(string path, string parameterName) =>
        string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Catalog path is required.", parameterName)
            : Path.GetFullPath(path);

    private sealed record RigCatalogDocument(
        int SchemaVersion,
        List<CharacterRigDefinition>? Rigs);

    private sealed record ClipCatalogDocument(
        int SchemaVersion,
        List<CharacterAnimationClipDefinition>? Clips);

    private sealed record SkinCatalogDocument(
        int SchemaVersion,
        List<CharacterSkinDefinition>? Skins);
}
