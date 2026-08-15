using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;

namespace HajimaoDesktopShop.Rendering.Combat;

public sealed record MaomaoCombatPose(
    string ClipId,
    SkeletalPose Pose);

public sealed record CustomerCombatPose(
    long CustomerEntityId,
    int X,
    int Y,
    string ClipId,
    SkeletalPose Pose);

public static class BusinessShopCombatChoreography
{
    private const int MaomaoX = 72;
    private const int ActorY = 152;
    private const int CustomerPathStartX = 122;
    private const int CustomerPathWidth = 268;

    public static MaomaoCombatPose CreateMaomaoPose(
        SkeletalAnimationCatalog catalog,
        StoreCombatSnapshot snapshot,
        long presentationFrame,
        bool reduceMotion)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(snapshot);
        var clipId = snapshot.Events.Any(combatEvent => combatEvent is ProductThrownEvent)
            ? "maomao-throw"
            : snapshot.Events.Any(combatEvent => combatEvent is CustomerServedEvent)
                ? "maomao-celebrate"
                : "maomao-idle";
        return new MaomaoCombatPose(
            clipId,
            SkeletalAnimator.Evaluate(
                catalog.Rigs["humanoid-v1"],
                catalog.Clips[clipId],
                presentationFrame,
                reduceMotion));
    }

    public static IReadOnlyList<CustomerCombatPose> CreateCustomerPoses(
        SkeletalAnimationCatalog catalog,
        StoreCombatSnapshot snapshot,
        long presentationFrame,
        bool reduceMotion,
        double simulationTickProgress = 0d) =>
        snapshot.State.Customers
            .OrderBy(customer => customer.EntityId)
            .Select(customer =>
            {
                var clipId = snapshot.Events.Any(combatEvent =>
                    combatEvent is ProductHitEvent hit && hit.CustomerEntityId == customer.EntityId)
                    ? "customer-hit"
                    : "customer-walk";
                return new CustomerCombatPose(
                    customer.EntityId,
                    CustomerX(PresentationPosition(customer, simulationTickProgress)),
                    ActorY,
                    clipId,
                    SkeletalAnimator.Evaluate(
                        catalog.Rigs["humanoid-v1"],
                        catalog.Clips[clipId],
                        presentationFrame,
                        reduceMotion));
            })
            .ToArray();

    private static int PresentationPosition(ActiveCustomerState customer, double tickProgress)
    {
        var clampedProgress = Math.Clamp(tickProgress, 0d, 1d);
        var movementModifier = Math.Clamp(1_000 - customer.SlowStrengthPermille, 100, 1_000);
        var movement = Math.Max(1, customer.MovementPermillePerTick * movementModifier / 1_000);
        return Math.Max(0, customer.PositionPermille - (int)Math.Round(movement * clampedProgress));
    }

    public static int CustomerX(int positionPermille) =>
        CustomerPathStartX + Math.Clamp(positionPermille, 0, 10_000) * CustomerPathWidth / 10_000;

    public static int MaomaoAnchorX => MaomaoX;
    public static int ActorAnchorY => ActorY;
}
