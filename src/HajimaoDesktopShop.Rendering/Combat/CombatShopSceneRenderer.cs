using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Characters;
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
    int FeedbackDrawCount,
    int GroundShadowDrawCount);

public sealed class CombatShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;
    private readonly SkeletalAnimationCatalog _catalog;
    private readonly SKImage _parts;
    private readonly StoreInteriorRenderer _interiors = new();
    private readonly ProductProjectileRenderer _projectiles = new();
    private readonly SKPaint _paint = new() { IsAntialias = false };
    private readonly SKTypeface _feedbackTypeface =
        SKTypeface.FromFamilyName("Microsoft YaHei UI", SKFontStyle.Bold) ?? SKTypeface.Default;
    private readonly SKFont _feedbackFont;

    public CombatShopSceneRenderer(SkeletalAnimationCatalog catalog, SKBitmap parts)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(parts);
        _parts = SKImage.FromBitmap(parts);
        _feedbackFont = new SKFont(_feedbackTypeface, 9f);
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
            return new CombatSceneRenderStats(0, 0, 0, 0, 0);
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
        var shadows = 0;
        var characters = 0;
        if (frame.Snapshot.IsStaffed)
        {
            DrawGroundShadow(
                canvas,
                BusinessShopCombatChoreography.MaomaoAnchorX,
                BusinessShopCombatChoreography.ActorAnchorY + 7,
                scaleX: 1f);
            shadows = 1;
            SkeletalCharacterRenderer.Draw(
                canvas,
                _parts,
                rig,
                skin,
                maomao.Pose,
                BusinessShopCombatChoreography.MaomaoAnchorX,
                BusinessShopCombatChoreography.ActorAnchorY);
            characters = 1;
        }
        var customerPoses = BusinessShopCombatChoreography.CreateCustomerPoses(
            _catalog,
            frame.Snapshot,
            frame.AnimationFrame,
            frame.ReduceMotion,
            tickProgress);
        foreach (var customer in customerPoses)
        {
            var visual = HumanCharacterVisualCatalog.Resolve(customer.ArchetypeId);
            DrawGroundShadow(
                canvas,
                customer.X,
                customer.Y + visual.GroundOffsetY + 7,
                visual.ScaleX);
            shadows++;
            HumanCharacterRenderer.Draw(
                canvas,
                _parts,
                _paint,
                rig,
                skin,
                customer.Pose,
                visual,
                customer.X,
                customer.Y,
                facingLeft: true);
            var liveCustomer = frame.Snapshot.State.Customers
                .SingleOrDefault(item => item.EntityId == customer.CustomerEntityId);
            if (customer.ShowDemand && liveCustomer is not null)
            {
                var head = HumanCharacterRenderer.HeadAnchor(
                    customer.Pose,
                    visual,
                    customer.X,
                    customer.Y,
                    facingLeft: true);
                DrawCustomerDemand(canvas, liveCustomer, head);
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
        var socket = maomao.Pose.Sockets["product_socket"];
        var projectileOrigin = new ProjectilePoint(
            BusinessShopCombatChoreography.MaomaoAnchorX + socket.WorldX,
            BusinessShopCombatChoreography.ActorAnchorY + socket.WorldY);
        var targetAnchors = customerPoses
            .Where(pose => frame.Snapshot.State.Customers.Any(customer => customer.EntityId == pose.CustomerEntityId))
            .ToDictionary(
                pose => pose.CustomerEntityId,
                pose =>
                {
                    var visual = HumanCharacterVisualCatalog.Resolve(pose.ArchetypeId);
                    var torso = pose.Pose.Bones["torso"];
                    return new ProjectilePoint(
                        (int)MathF.Round(pose.X - torso.WorldX * visual.ScaleX),
                        (int)MathF.Round(pose.Y + visual.GroundOffsetY + torso.WorldY * visual.ScaleY));
                });
        var projectileCount = releaseReached
            ? _projectiles.Draw(
                canvas,
                frame.Snapshot.State,
                frame.ProductIconKeys,
                projectileOrigin,
                targetAnchors,
                tickProgress)
            : 0;
        var feedback = DrawFeedback(
            canvas,
            frame.Snapshot,
            customerPoses,
            frame.AnimationFrame,
            frame.ReduceMotion);
        canvas.Restore();
        return new CombatSceneRenderStats(backgrounds, characters, projectileCount, feedback, shadows);
    }

    private void DrawGroundShadow(SKCanvas canvas, int x, int y, float scaleX)
    {
        _paint.Color = SKColor.Parse("#242229").WithAlpha(190);
        var halfWidth = Math.Max(7, (int)MathF.Round(10 * scaleX));
        canvas.DrawRect(x - halfWidth, y, halfWidth * 2, 3, _paint);
        canvas.DrawRect(x - halfWidth + 3, y - 1, (halfWidth - 3) * 2, 5, _paint);
    }

    private int DrawFeedback(
        SKCanvas canvas,
        StoreCombatSnapshot snapshot,
        IReadOnlyList<CustomerCombatPose> customerPoses,
        long animationFrame,
        bool reduceMotion)
    {
        var count = 0;
        var feedbackFrame = reduceMotion ? 0 : SkeletalAnimator.LogicalFrame(animationFrame, reduceMotion: false);
        var rise = reduceMotion ? 0 : feedbackFrame * 10 / 23;
        foreach (var hit in snapshot.Events.OfType<ProductHitEvent>())
        {
            var customer = snapshot.Events.OfType<CustomerServedEvent>()
                .Any(served => served.CustomerEntityId == hit.CustomerEntityId);
            var feedbackColor = customer ? SKColor.Parse("#F1B844") : SKColor.Parse("#E15A5A");
            var target = snapshot.State.Customers
                .SingleOrDefault(customer => customer.EntityId == hit.CustomerEntityId);
            var rendered = customerPoses
                .SingleOrDefault(pose => pose.CustomerEntityId == hit.CustomerEntityId);
            var x = target is not null && rendered is not null
                ? rendered.X
                : BusinessShopCombatChoreography.CustomerX(hit.TargetPositionPermille);
            var feedbackY = 108;
            if (rendered is not null)
            {
                var visual = HumanCharacterVisualCatalog.Resolve(rendered.ArchetypeId);
                var head = HumanCharacterRenderer.HeadAnchor(
                    rendered.Pose,
                    visual,
                    x,
                    rendered.Y,
                    facingLeft: true);
                feedbackY = (int)MathF.Round(head.Y);
            }

            _paint.Color = SKColor.Parse("#241A1D");
            canvas.DrawText(
                $"-{hit.Damage}",
                x - 7,
                feedbackY - 12 - rise + 1,
                SKTextAlign.Left,
                _feedbackFont,
                _paint);
            _paint.Color = feedbackColor;
            canvas.DrawText(
                $"-{hit.Damage}",
                x - 8,
                feedbackY - 12 - rise,
                SKTextAlign.Left,
                _feedbackFont,
                _paint);
            count++;
        }

        foreach (var served in snapshot.Events.OfType<CustomerServedEvent>())
        {
            _paint.Color = SKColor.Parse("#F1B844");
            var impact = snapshot.Events.OfType<ProductHitEvent>()
                .LastOrDefault(hit => hit.CustomerEntityId == served.CustomerEntityId);
            var impactX = impact is null
                ? 252 + (int)(served.CustomerEntityId % 4) * 6
                : BusinessShopCombatChoreography.CustomerX(impact.TargetPositionPermille);
            var x = Math.Clamp(impactX - 22, 4, LogicalWidth - 58);
            _paint.Color = SKColor.Parse("#302A20").WithAlpha(220);
            canvas.DrawRect(x - 3, 64, 58, 17, _paint);
            _paint.Color = SKColor.Parse("#F1B844");
            canvas.DrawText("招待成功", x, 76, SKTextAlign.Left, _feedbackFont, _paint);
            canvas.DrawRect(impactX, 85, 4, 4, _paint);
            count++;
        }

        var awardedDrops = snapshot.DropRolls.Count(roll => roll.Awarded && roll.ProductId is not null);
        if (awardedDrops > 0)
        {
            var newUnlocks = snapshot.DropRolls.Count(roll => roll.IsNewUnlock);
            var lastServed = snapshot.Events.OfType<CustomerServedEvent>().LastOrDefault();
            var impact = lastServed is null
                ? null
                : snapshot.Events.OfType<ProductHitEvent>()
                    .LastOrDefault(hit => hit.CustomerEntityId == lastServed.CustomerEntityId);
            var impactX = impact is null
                ? 286
                : BusinessShopCombatChoreography.CustomerX(impact.TargetPositionPermille);
            var x = Math.Clamp(impactX - 18, 4, LogicalWidth - 52);
            var label = newUnlocks > 0
                ? "解锁新品！"
                : awardedDrops == 1 ? "商品 +1" : $"商品 +{awardedDrops}";
            _paint.Color = SKColor.Parse("#2F233A").WithAlpha(220);
            canvas.DrawRect(x - 3, 82, 52, 17, _paint);
            _paint.Color = SKColor.Parse("#B784E8");
            canvas.DrawText(label, x, 94, SKTextAlign.Left, _feedbackFont, _paint);
            count++;
        }

        return count;
    }

    private void DrawCustomerDemand(SKCanvas canvas, ActiveCustomerState customer, SKPoint head)
    {
        const int width = 22;
        var maximum = Math.Max(customer.DemandHp, customer.MaximumDemandHp);
        var filled = Math.Clamp(customer.DemandHp * width / Math.Max(1, maximum), 1, width);
        var x = (int)MathF.Round(head.X);
        var y = (int)MathF.Round(head.Y) - 6;
        _paint.Color = SKColor.Parse("#302F36");
        canvas.DrawRect(x - width / 2 - 1, y, width + 2, 5, _paint);
        _paint.Color = customer.DemandHp * 4 <= maximum
            ? SKColor.Parse("#F1B844")
            : SKColor.Parse("#70C97A");
        canvas.DrawRect(x - width / 2, y + 1, filled, 3, _paint);
    }

    public void Dispose()
    {
        _interiors.Dispose();
        _projectiles.Dispose();
        _parts.Dispose();
        _feedbackFont.Dispose();
        _feedbackTypeface.Dispose();
        _paint.Dispose();
    }
}
