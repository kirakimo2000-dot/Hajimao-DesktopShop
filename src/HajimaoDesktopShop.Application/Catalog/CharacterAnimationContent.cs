namespace HajimaoDesktopShop.Application.Catalog;

public sealed record CharacterAnimationContent(
    IReadOnlyList<CharacterRigDefinition> Rigs,
    IReadOnlyList<CharacterAnimationClipDefinition> Clips,
    IReadOnlyList<CharacterSkinDefinition> Skins);

public sealed record CharacterRigDefinition(
    string Id,
    IReadOnlyList<CharacterBoneDefinition> Bones,
    IReadOnlyList<CharacterSocketDefinition> Sockets);

public sealed record CharacterBoneDefinition(
    string Id,
    string? ParentId,
    float PivotX,
    float PivotY,
    int ZIndex);

public sealed record CharacterSocketDefinition(
    string Id,
    string BoneId,
    float OffsetX,
    float OffsetY);

public sealed record CharacterAnimationClipDefinition(
    string Id,
    string RigId,
    int LogicalFrameCount,
    IReadOnlyDictionary<string, IReadOnlyList<CharacterBoneKeyframeDefinition>> BoneTracks,
    IReadOnlyList<CharacterAnimationMarkerDefinition> Markers);

public sealed record CharacterBoneKeyframeDefinition(
    int Frame,
    float TranslationX = 0,
    float TranslationY = 0,
    float RotationDegrees = 0,
    float ScaleX = 1,
    float ScaleY = 1);

public sealed record CharacterAnimationMarkerDefinition(int Frame, string Id);

public sealed record CharacterSkinDefinition(
    string Id,
    string RigId,
    string AssetPath,
    IReadOnlyDictionary<string, CharacterSkinPartDefinition> Parts);

public sealed record CharacterSkinPartDefinition(
    int SourceX,
    int SourceY,
    int Width,
    int Height,
    float PivotX,
    float PivotY);
