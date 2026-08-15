namespace HajimaoDesktopShop.Rendering.Animation;

public sealed record RigBone(
    string Id,
    string? ParentId,
    float PivotX,
    float PivotY,
    int ZIndex);

public sealed record RigSocket(
    string Id,
    string BoneId,
    float OffsetX,
    float OffsetY);

public sealed class SkeletalRig
{
    public SkeletalRig(
        string id,
        IReadOnlyList<RigBone> bones,
        IReadOnlyList<RigSocket> sockets)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Rig ID is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(bones);
        ArgumentNullException.ThrowIfNull(sockets);

        var boneMap = BuildBoneMap(bones);
        ValidateHierarchy(boneMap);
        var socketMap = BuildSocketMap(sockets, boneMap);

        Id = id.Trim();
        Bones = Array.AsReadOnly(bones.ToArray());
        Sockets = Array.AsReadOnly(sockets.ToArray());
        BoneMap = boneMap;
        SocketMap = socketMap;
    }

    public string Id { get; }

    public IReadOnlyList<RigBone> Bones { get; }

    public IReadOnlyList<RigSocket> Sockets { get; }

    internal IReadOnlyDictionary<string, RigBone> BoneMap { get; }

    internal IReadOnlyDictionary<string, RigSocket> SocketMap { get; }

    private static IReadOnlyDictionary<string, RigBone> BuildBoneMap(
        IReadOnlyList<RigBone> bones)
    {
        if (bones.Count == 0)
        {
            throw new ArgumentException("A rig requires at least one bone.", nameof(bones));
        }

        var map = new Dictionary<string, RigBone>(StringComparer.Ordinal);
        foreach (var bone in bones)
        {
            if (string.IsNullOrWhiteSpace(bone.Id) || !map.TryAdd(bone.Id, bone))
            {
                throw new ArgumentException("Rig bone IDs must be non-empty and unique.", nameof(bones));
            }
        }

        return map;
    }

    private static void ValidateHierarchy(IReadOnlyDictionary<string, RigBone> bones)
    {
        foreach (var bone in bones.Values)
        {
            if (bone.ParentId is not null && !bones.ContainsKey(bone.ParentId))
            {
                throw new ArgumentException($"Bone '{bone.Id}' has unknown parent '{bone.ParentId}'.");
            }

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = bone;
            while (current.ParentId is string parentId)
            {
                if (!visited.Add(current.Id))
                {
                    throw new ArgumentException($"Rig contains a cycle at bone '{current.Id}'.");
                }

                current = bones[parentId];
            }
        }
    }

    private static IReadOnlyDictionary<string, RigSocket> BuildSocketMap(
        IReadOnlyList<RigSocket> sockets,
        IReadOnlyDictionary<string, RigBone> bones)
    {
        var map = new Dictionary<string, RigSocket>(StringComparer.Ordinal);
        foreach (var socket in sockets)
        {
            if (string.IsNullOrWhiteSpace(socket.Id) || !map.TryAdd(socket.Id, socket))
            {
                throw new ArgumentException("Rig socket IDs must be non-empty and unique.", nameof(sockets));
            }

            if (!bones.ContainsKey(socket.BoneId))
            {
                throw new ArgumentException(
                    $"Socket '{socket.Id}' references unknown bone '{socket.BoneId}'.",
                    nameof(sockets));
            }
        }

        return map;
    }
}
