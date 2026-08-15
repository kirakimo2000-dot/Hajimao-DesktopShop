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
        double simulationTickProgress = 0d)
    {
        var count = 0;
        foreach (var projectile in state.Projectiles.OrderBy(item => item.EntityId))
        {
            var target = state.Customers.SingleOrDefault(customer => customer.EntityId == projectile.TargetCustomerEntityId);
            if (target is null)
            {
                continue;
            }

            var point = Interpolate(
                projectile,
                BusinessShopCombatChoreography.MaomaoAnchorX + 12,
                BusinessShopCombatChoreography.ActorAnchorY - 30,
                BusinessShopCombatChoreography.CustomerX(target.PositionPermille),
                BusinessShopCombatChoreography.ActorAnchorY - 24,
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
            canvas.DrawImage(
                _atlasImage,
                new SKRect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height),
                new SKRect(point.X - 6, point.Y - 6, point.X + 6, point.Y + 6),
                PixelSampling,
                _paint);
            count++;
        }

        return count;
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
        var arcY = MathF.Sin(MathF.PI * progress) * 20f;
        return new ProjectilePoint((int)MathF.Round(x), (int)MathF.Round(linearY - arcY));
    }

    public void Dispose()
    {
        _atlasImage.Dispose();
        _atlas.Dispose();
        _paint.Dispose();
    }
}
