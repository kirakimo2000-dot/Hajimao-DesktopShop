using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Interiors;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Combat;

public sealed record CombatShopSceneFrame(
    StoreCombatSnapshot Snapshot,
    string BackgroundAssetPath,
    IReadOnlyDictionary<string, string> ProductIconKeys,
    long AnimationFrame,
    bool ReduceMotion);

public sealed record CombatSceneRenderStats(
    int BackgroundDrawCount,
    int CharacterDrawCount,
    int ProjectileDrawCount,
    int FeedbackDrawCount);

public sealed class CombatShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;
    private readonly SkeletalAnimationCatalog _catalog;
    private readonly SKBitmap _parts;
    private readonly StoreInteriorRenderer _interiors = new();
    private readonly ProductProjectileRenderer _projectiles = new();
    private readonly SKPaint _paint = new() { IsAntialias = false };
    private readonly SKTypeface _feedbackTypeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
    private readonly SKFont _feedbackFont;

    public CombatShopSceneRenderer(SkeletalAnimationCatalog catalog, SKBitmap parts)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _parts = parts ?? throw new ArgumentNullException(nameof(parts));
        _feedbackFont = new SKFont(_feedbackTypeface, 7f);
    }

    public CombatSceneRenderStats Render(
        SKCanvas canvas,
        SKImageInfo target,
        CombatShopSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        canvas.Clear(SKColor.Parse("#17191D"));
        if (target.Width <= 0 || target.Height <= 0)
        {
            return new CombatSceneRenderStats(0, 0, 0, 0);
        }

        var scale = Math.Max(1, Math.Min(target.Width / LogicalWidth, target.Height / LogicalHeight));
        canvas.Save();
        canvas.Translate(
            (target.Width - LogicalWidth * scale) / 2,
            (target.Height - LogicalHeight * scale) / 2);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, LogicalWidth, LogicalHeight));

        var backgrounds = _interiors.Draw(canvas, frame.BackgroundAssetPath, LogicalWidth, LogicalHeight);
        var rig = _catalog.Rigs["humanoid-v1"];
        var skin = _catalog.Skins["maomao-default"];
        var maomao = BusinessShopCombatChoreography.CreateMaomaoPose(
            _catalog,
            frame.Snapshot,
            frame.AnimationFrame,
            frame.ReduceMotion);
        var logicalSubframe = Math.Abs(frame.AnimationFrame % 24);
        var tickProgress = logicalSubframe / 23d;
        SkeletalCharacterRenderer.Draw(
            canvas,
            _parts,
            rig,
            skin,
            maomao.Pose,
            BusinessShopCombatChoreography.MaomaoAnchorX,
            BusinessShopCombatChoreography.ActorAnchorY);
        var characters = 1;
        foreach (var customer in BusinessShopCombatChoreography.CreateCustomerPoses(
                     _catalog,
                     frame.Snapshot,
                     frame.AnimationFrame,
                     frame.ReduceMotion,
                     tickProgress))
        {
            SkeletalCharacterRenderer.Draw(
                canvas,
                _parts,
                rig,
                skin,
                customer.Pose,
                customer.X,
                customer.Y,
                facingLeft: true,
                tint: CustomerTint(customer.ArchetypeId));
            var liveCustomer = frame.Snapshot.State.Customers
                .SingleOrDefault(item => item.EntityId == customer.CustomerEntityId);
            if (customer.ShowDemand && liveCustomer is not null)
            {
                DrawCustomerDemand(canvas, liveCustomer, customer.X, customer.Y);
            }
            characters++;
        }

        var throwClip = _catalog.Clips["maomao-throw"];
        var releaseFrame = throwClip.Markers.Single(marker => marker.Id == "release_product").Frame;
        var isThrowing = frame.Snapshot.Events.Any(combatEvent => combatEvent is ProductThrownEvent);
        var releaseReached = frame.ReduceMotion || !isThrowing || frame.AnimationFrame >= releaseFrame;
        if (isThrowing && releaseReached && !frame.ReduceMotion)
        {
            tickProgress = Math.Clamp(
                (frame.AnimationFrame - releaseFrame) / (double)Math.Max(1, 23 - releaseFrame),
                0d,
                1d);
        }
        var projectileCount = releaseReached
            ? _projectiles.Draw(canvas, frame.Snapshot.State, frame.ProductIconKeys, tickProgress)
            : 0;
        var feedback = DrawFeedback(canvas, frame.Snapshot);
        canvas.Restore();
        return new CombatSceneRenderStats(backgrounds, characters, projectileCount, feedback);
    }

    private int DrawFeedback(SKCanvas canvas, StoreCombatSnapshot snapshot)
    {
        var count = 0;
        foreach (var hit in snapshot.Events.OfType<ProductHitEvent>())
        {
            var customer = snapshot.Events.OfType<CustomerServedEvent>()
                .Any(served => served.CustomerEntityId == hit.CustomerEntityId);
            _paint.Color = customer ? SKColor.Parse("#F1B844") : SKColor.Parse("#E15A5A");
            var x = BusinessShopCombatChoreography.CustomerX(5_000) + (int)(hit.CustomerEntityId % 5) * 2;
            canvas.DrawRect(x - 5, 96, 10, 4, _paint);
            canvas.DrawText($"-{hit.Damage}", x - 8, 92, SKTextAlign.Left, _feedbackFont, _paint);
            count++;
        }

        foreach (var served in snapshot.Events.OfType<CustomerServedEvent>())
        {
            _paint.Color = SKColor.Parse("#F1B844");
            var x = 252 + (int)(served.CustomerEntityId % 4) * 6;
            canvas.DrawRect(x, 78, 4, 4, _paint);
            canvas.DrawRect(x + 7, 85, 3, 3, _paint);
            count++;
        }

        foreach (var drop in snapshot.DropRolls.Where(roll => roll.Awarded && roll.ProductId is not null))
        {
            _paint.Color = SKColor.Parse("#B784E8");
            canvas.DrawRect(286 + (count % 3) * 6, 72, 4, 8, _paint);
            count++;
        }

        return count;
    }

    private void DrawCustomerDemand(SKCanvas canvas, ActiveCustomerState customer, int x, int y)
    {
        const int width = 28;
        var maximum = Math.Max(customer.DemandHp, customer.MaximumDemandHp);
        var filled = Math.Clamp(customer.DemandHp * width / Math.Max(1, maximum), 1, width);
        _paint.Color = SKColor.Parse("#302F36");
        canvas.DrawRect(x - width / 2 - 1, y - 47, width + 2, 5, _paint);
        _paint.Color = customer.DemandHp * 4 <= maximum
            ? SKColor.Parse("#F1B844")
            : SKColor.Parse("#70C97A");
        canvas.DrawRect(x - width / 2, y - 46, filled, 3, _paint);
    }

    private static SKColor CustomerTint(string archetypeId)
    {
        var colors = new[]
        {
            SKColor.Parse("#E6B6A8"),
            SKColor.Parse("#A8C9E6"),
            SKColor.Parse("#B7DEA4"),
            SKColor.Parse("#E1C188"),
            SKColor.Parse("#C1ACE5"),
            SKColor.Parse("#E3A8C8")
        };
        var hash = 17;
        foreach (var character in archetypeId)
        {
            hash = unchecked((hash * 31) + character);
        }

        return colors[(hash & int.MaxValue) % colors.Length];
    }

    public void Dispose()
    {
        _interiors.Dispose();
        _projectiles.Dispose();
        _feedbackFont.Dispose();
        _feedbackTypeface.Dispose();
        _paint.Dispose();
    }
}
