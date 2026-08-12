namespace HajimaoDesktopShop.Rendering.PixelArt;

public static class ContentSpriteKey
{
    private static readonly IReadOnlyDictionary<string, int> ProductCategoryFrames =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["beverage"] = 0,
            ["snack"] = 3,
            ["prepared"] = 6,
            ["bakery"] = 1,
            ["dairy"] = 4,
            ["frozen"] = 8,
            ["household"] = 9,
            ["personal"] = 7,
            ["wellness"] = 5,
            ["stationery"] = 2,
            ["home"] = 9,
            ["staple"] = 2
        };

    private static readonly IReadOnlyDictionary<string, int> FacadePalettes =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["convenience"] = 0,
            ["commuter"] = 1,
            ["discount"] = 2,
            ["warehouse"] = 3,
            ["lifestyle"] = 4,
            ["health"] = 5,
            ["premium"] = 6,
            ["variety"] = 7
        };

    public static ProductSpriteVariant ResolveProduct(string key)
    {
        var segments = Split(key, "product", minimumSegments: 3);
        var category = segments[1];
        var frame = ProductCategoryFrames.GetValueOrDefault(category, (int)(StableHash(category) % 10));
        return new ProductSpriteVariant(frame, (int)(StableHash(key) % 8));
    }

    public static FacadeSpriteVariant ResolveFacade(string key)
    {
        var segments = Split(key, "facade", minimumSegments: 3);
        var format = segments[1];
        if (!FacadePalettes.TryGetValue(format, out var palette))
        {
            throw new ArgumentException($"Unknown facade format in key '{key}'.", nameof(key));
        }

        var style = segments[^1];
        if (style.Length != 1 || style[0] is < 'a' or > 'd')
        {
            throw new ArgumentException($"Unknown facade style in key '{key}'.", nameof(key));
        }

        return new FacadeSpriteVariant(palette, style[0] - 'a');
    }

    public static EmployeeSpriteVariant ResolveEmployee(string key)
    {
        var segments = Split(key, "employee", minimumSegments: 2);
        var code = segments[1];
        if (code.Length != 3
            || code[0] is < 'a' or > 'l'
            || !int.TryParse(code.AsSpan(1), out var detail)
            || detail is < 1 or > 8)
        {
            throw new ArgumentException($"Unknown employee appearance key '{key}'.", nameof(key));
        }

        return new EmployeeSpriteVariant(
            code[0] - 'a',
            detail - 1,
            PixelArtBudget.CharacterAnimationFrameCount,
            PixelArtBudget.StoredCharacterCelCount);
    }

    private static string[] Split(string key, string prefix, int minimumSegments)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Content sprite key is required.", nameof(key));
        }

        var segments = key.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < minimumSegments || !string.Equals(segments[0], prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Content sprite key '{key}' is invalid.", nameof(key));
        }

        return segments;
    }

    private static uint StableHash(string value)
    {
        var hash = 2_166_136_261U;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16_777_619U;
        }

        return hash;
    }
}

public sealed record ProductSpriteVariant(int FrameIndex, int PaletteIndex);

public sealed record FacadeSpriteVariant(int PaletteIndex, int AwningIndex);

public sealed record EmployeeSpriteVariant(
    int PaletteIndex,
    int DetailIndex,
    int LogicalFrameCount,
    int StoredCelCount);
