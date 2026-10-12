using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.PixelArt;

public static class StorefrontTextureSet
{
    public const int Width = 224;
    public const int Height = 102;

    public static string FileName(FacadeGeometryKind kind) => kind switch
    {
        FacadeGeometryKind.Convenience => "convenience.png",
        FacadeGeometryKind.Commuter => "commuter.png",
        FacadeGeometryKind.Discount => "discount.png",
        FacadeGeometryKind.Warehouse => "warehouse.png",
        FacadeGeometryKind.Lifestyle => "lifestyle.png",
        FacadeGeometryKind.Health => "health.png",
        FacadeGeometryKind.Premium => "premium.png",
        FacadeGeometryKind.Variety => "variety.png",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static IReadOnlyDictionary<FacadeGeometryKind, SKBitmap> Load(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var textures = new Dictionary<FacadeGeometryKind, SKBitmap>();
        try
        {
            foreach (var kind in Enum.GetValues<FacadeGeometryKind>())
            {
                var path = Path.Combine(directory, FileName(kind));
                var bitmap = SKBitmap.Decode(path)
                    ?? throw new InvalidDataException($"Storefront texture is unreadable: {path}");
                if (bitmap.Width != Width || bitmap.Height != Height)
                {
                    bitmap.Dispose();
                    throw new InvalidDataException(
                        $"Storefront texture '{path}' must be {Width}x{Height} pixels.");
                }

                textures.Add(kind, bitmap);
            }

            return textures;
        }
        catch
        {
            foreach (var bitmap in textures.Values)
            {
                bitmap.Dispose();
            }

            throw;
        }
    }
}
