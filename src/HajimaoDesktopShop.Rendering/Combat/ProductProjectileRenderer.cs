using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Combat;

public readonly record struct ProjectilePoint(int X, int Y);

public sealed class ProductProjectileRenderer : IDisposable
{
    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);
    private readonly PixelSpriteAtlas _atlas = PixelSpriteAtlas.LoadDefault();
    private readonly SKImage _atlasImage;
    private readonly SKPaint _paint = new() { IsAntialias = false };

    public ProductProjectileRenderer()
    {
        _atlasImage = SKImage.FromBitmap(_atlas.Bitmap);
    }

    public int Draw(
        SKCanvas canvas,
        StoreCombatState state,
        IReadOnlyDictionary<string, string> productIconKeys,
        ProjectilePoint origin,
        IReadOnlyDictionary<long, ProjectilePoint> targetAnchors,
        double simulationTickProgress = 0d)
    {
        ArgumentNullException.ThrowIfNull(targetAnchors);
        var count = 0;
        foreach (var projectile in state.Projectiles.OrderBy(item => item.EntityId))
        {
            if (!targetAnchors.TryGetValue(projectile.TargetCustomerEntityId, out var target))
            {
                continue;
            }

            var point = Interpolate(
                projectile,
                origin.X,
                origin.Y,
                target.X,
                target.Y,
                simulationTickProgress);
            var iconKey = productIconKeys.GetValueOrDefault(projectile.ProductId, "product-beverage-water");
            ProductSpriteVariant variant;
            try
            {
                variant = ContentSpriteKey.ResolveProduct(iconKey);
            }
            catch (ArgumentException)
            {
                variant = new ProductSpriteVariant(0, 0);
            }

            var frame = _atlas.ProductFrames[variant.FrameIndex];
            DrawTrail(canvas, point, target.X);
            canvas.DrawImage(
                _atlasImage,
                new SKRect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height),
                new SKRect(point.X - 4, point.Y - 4, point.X + 4, point.Y + 4),
                PixelSampling,
                _paint);
            count++;
        }

        return count;
    }

    private void DrawTrail(SKCanvas canvas, ProjectilePoint point, int targetX)
    {
        var direction = targetX >= point.X
            ? -1
            : 1;
        var color = SKColor.Parse("#F7D88B");
        for (var index = 0; index < 3; index++)
        {
            var distance = 9 + index * 3;
            _paint.Color = color.WithAlpha((byte)(190 - index * 45));
            var size = index == 0 ? 2 : 1;
            canvas.DrawRect(
                point.X + direction * distance - size / 2,
                point.Y + index - size / 2,
                size,
                size,
                _paint);
        }

        _paint.Color = SKColors.White;
    }

    public static ProjectilePoint Interpolate(
        ProductProjectileState projectile,
        int originX,
        int originY,
        int targetX,
        int targetY,
        double simulationTickProgress = 0d)
    {
        var total = Math.Max(projectile.TotalTravelTicks, projectile.RemainingTravelTicks);
        var remaining = Math.Max(0d, projectile.RemainingTravelTicks - Math.Clamp(simulationTickProgress, 0d, 1d));
        var progress = total <= 0
            ? 1f
            : Math.Clamp(1f - ((float)remaining / total), 0f, 1f);
        var x = originX + ((targetX - originX) * progress);
        var linearY = originY + ((targetY - originY) * progress);
        var arcY = MathF.Sin(MathF.PI * progress) * 8f;
        return new ProjectilePoint((int)MathF.Round(x), (int)MathF.Round(linearY - arcY));
    }

    public void Dispose()
    {
        _atlasImage.Dispose();
        _atlas.Dispose();
        _paint.Dispose();
    }
}
