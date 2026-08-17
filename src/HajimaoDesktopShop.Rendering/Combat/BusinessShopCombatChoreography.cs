using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;

namespace HajimaoDesktopShop.Rendering.Combat;

public sealed record MaomaoCombatPose(
    string ClipId,
    SkeletalPose Pose);

public sealed record CustomerCombatPose(
    long CustomerEntityId,
    string ArchetypeId,
    int X,
    int Y,
    string ClipId,
    bool ShowDemand,
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
        var isThrowing = snapshot.Events.Any(combatEvent => combatEvent is ProductThrownEvent);
        var actionFrame = Math.Clamp(presentationFrame, 0, 23);
        var clipId = isThrowing
            ? actionFrame < 8
                ? "maomao-wind-up"
                : actionFrame < 16
                    ? "maomao-throw"
                    : "maomao-recovery"
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
        CreateLiveCustomerPoses(catalog, snapshot, presentationFrame, reduceMotion, simulationTickProgress)
            .Concat(CreateDeparturePoses(catalog, snapshot, presentationFrame, reduceMotion))
            .ToArray();

    private static IEnumerable<CustomerCombatPose> CreateLiveCustomerPoses(
        SkeletalAnimationCatalog catalog,
        StoreCombatSnapshot snapshot,
        long presentationFrame,
        bool reduceMotion,
        double simulationTickProgress) =>
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
                    customer.ArchetypeId,
                    CustomerX(PresentationPosition(customer, simulationTickProgress)),
                    ActorY,
                    clipId,
                    ShowDemand: true,
                    SkeletalAnimator.Evaluate(
                        catalog.Rigs["humanoid-v1"],
                        catalog.Clips[clipId],
                        presentationFrame,
                        reduceMotion));
            });

    private static IEnumerable<CustomerCombatPose> CreateDeparturePoses(
        SkeletalAnimationCatalog catalog,
        StoreCombatSnapshot snapshot,
        long presentationFrame,
        bool reduceMotion)
    {
        var liveIds = snapshot.State.Customers
            .Select(customer => customer.EntityId)
            .ToHashSet();
        foreach (var served in snapshot.Events.OfType<CustomerServedEvent>()
                     .Where(served => !liveIds.Contains(served.CustomerEntityId)))
        {
            yield return CreateDeparturePose(
                catalog,
                served.CustomerEntityId,
                served.ArchetypeId,
                positionPermille: 2_500,
                "customer-served",
                presentationFrame,
                reduceMotion);
        }

        foreach (var escaped in snapshot.Events.OfType<CustomerEscapedEvent>()
                     .Where(escaped => !liveIds.Contains(escaped.CustomerEntityId)))
        {
            yield return CreateDeparturePose(
                catalog,
                escaped.CustomerEntityId,
                escaped.ArchetypeId,
                positionPermille: 0,
                "customer-leave",
                presentationFrame,
                reduceMotion);
        }
    }

    private static CustomerCombatPose CreateDeparturePose(
        SkeletalAnimationCatalog catalog,
        long entityId,
        string archetypeId,
        int positionPermille,
        string clipId,
        long presentationFrame,
        bool reduceMotion) =>
        new(
            entityId,
            archetypeId,
            CustomerX(positionPermille),
            ActorY,
            clipId,
            ShowDemand: false,
            SkeletalAnimator.Evaluate(
                catalog.Rigs["humanoid-v1"],
                catalog.Clips[clipId],
                presentationFrame,
                reduceMotion));

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
