namespace HajimaoDesktopShop.Rendering.Animation;

public sealed record SkinPart(
    string AssetId,
    int SourceX,
    int SourceY,
    int Width,
    int Height,
    float PivotX,
    float PivotY);

public sealed record CharacterSkin(
    string Id,
    string RigId,
    IReadOnlyDictionary<string, SkinPart> Parts);
