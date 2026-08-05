using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Rendering.Tests.Interactions;

public sealed class BusinessShopInteractionMapTests
{
    [Fact]
    public void HitTest_ReturnsStableShelfTargetsAndUsesHalfOpenBounds()
    {
        var frame = CreateFrame();
        var targets = BusinessShopInteractionMap.CreateTargets(frame);
        var shelf = Assert.Single(
            targets,
            target => target.Kind == BusinessShopInteractionKind.Shelf
                && target.Key == "ambient");

        Assert.Equal(
            shelf,
            BusinessShopInteractionMap.HitTest(frame, shelf.Bounds.X, shelf.Bounds.Y));
        Assert.Null(BusinessShopInteractionMap.HitTest(
            frame,
            shelf.Bounds.Right,
            shelf.Bounds.Y));
        Assert.Null(BusinessShopInteractionMap.HitTest(
            frame,
            shelf.Bounds.Right - 18,
            shelf.Bounds.Bottom));
        Assert.Equal(
            ["ambient", "chilled", "frozen"],
            targets
                .Where(target => target.Kind == BusinessShopInteractionKind.Shelf)
                .Select(target => target.Key));
    }

    [Fact]
    public void EmployeeTargets_FollowVisibleActorAnimationPoses()
    {
        var origin = BusinessShopInteractionMap.CreateTargets(CreateFrame(animationFrame: 0));
        var advanced = BusinessShopInteractionMap.CreateTargets(CreateFrame(animationFrame: 8));

        Assert.Equal(
            ["cashier", "restocker", "helper"],
            origin
                .Where(target => target.Kind == BusinessShopInteractionKind.Employee)
                .Select(target => target.Key));
        Assert.NotEqual(
            origin.Single(target => target.Key == "cashier").Bounds.X,
            advanced.Single(target => target.Key == "cashier").Bounds.X);
        Assert.NotEqual(
            origin.Single(target => target.Key == "restocker").Bounds.X,
            advanced.Single(target => target.Key == "restocker").Bounds.X);
    }

    [Fact]
    public void HitTest_PrioritizesEmployeeOverOverlappingShelf()
    {
        var frame = CreateFrame();
        var cashier = BusinessShopInteractionMap.CreateTargets(frame)
            .Single(target => target.Key == "cashier");

        var hit = BusinessShopInteractionMap.HitTest(
            frame,
            cashier.Bounds.X + 12,
            cashier.Bounds.Y + 4);

        Assert.NotNull(hit);
        Assert.Equal(BusinessShopInteractionKind.Employee, hit.Kind);
        Assert.Equal("cashier", hit.Key);
    }

    [Fact]
    public void InteractionTarget_RejectsBlankKeys()
    {
        Assert.Throws<ArgumentException>(() => new BusinessShopInteractionTarget(
            BusinessShopInteractionKind.Shelf,
            " ",
            new LogicalPixelRect(0, 0, 1, 1)));
    }

    private static BusinessShopSceneFrame CreateFrame(int animationFrame = 0)
    {
        var employees = new EmployeeOperationsSnapshot(
            1,
            1,
            [],
            [
                Employee("cashier", EmployeeRole.Cashier),
                Employee("restocker", EmployeeRole.Restocker),
                Employee("helper", EmployeeRole.SalesAssistant)
            ]);
        var business = new BusinessSnapshot(
            1,
            0,
            100_000,
            [
                new BusinessStoreSnapshot(
                    "corner-store",
                    "街角便利店",
                    0,
                    0,
                    0,
                    [
                        new ProductSnapshot("water", "矿泉水", 100, 200, 0, 20, "ambient"),
                        new ProductSnapshot("milk", "牛奶", 200, 300, 0, 20, "chilled"),
                        new ProductSnapshot("ice", "冰淇淋", 300, 500, 0, 20, "frozen")
                    ])
            ]);
        var snapshot = new BusinessSimulationSnapshot(
            0,
            business,
            [
                new StoreOperationsSnapshot(
                    "corner-store",
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    new DemandBreakdown(10_000, 0, 0, 0, 0, 0, 10_000))
            ],
            employees,
            new CommercialStreetSnapshot(
                CommercialStreetTier.Corner,
                StreetWeather.Clear,
                10_000,
                0,
                0,
                [new CommercialStreetStoreSnapshot("corner-store", "街角便利店", 10_000, 10_000)]));
        return new BusinessShopSceneFrame(snapshot, "corner-store", animationFrame);
    }

    private static EmployeeOperationsEmployeeSnapshot Employee(string id, EmployeeRole role) =>
        new(
            id,
            id,
            role,
            1_000,
            1_000,
            6_000,
            0,
            1_000,
            1_000,
            "corner-store",
            0,
            1_440,
            true);
}
