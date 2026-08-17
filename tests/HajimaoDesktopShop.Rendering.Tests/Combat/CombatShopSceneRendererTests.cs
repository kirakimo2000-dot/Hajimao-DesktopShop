using System.Security.Cryptography;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Combat;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.Combat;

public sealed class CombatShopSceneRendererTests
{
    [Fact]
    public async Task Render_DrawsOneInteriorBackgroundAndZeroShelves()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);

        var stats = renderer.Render(canvas, bitmap.Info, Frame(State()));

        Assert.Equal(1, stats.BackgroundDrawCount);
        Assert.Equal(1, stats.CharacterDrawCount);
    }

    [Fact]
    public async Task Render_CustomerPositionComesFromCombatState()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        var left = RenderHash(renderer, State(Customer(position: 2_000)));
        var right = RenderHash(renderer, State(Customer(position: 8_000)));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public async Task Choreography_ThrowUsesWindUpReleaseAndRecoveryWithinOne24FrameAction()
    {
        using var resources = await Resources.LoadAsync();
        var thrown = new ProductThrownEvent(2, "water", 1);
        var snapshot = Snapshot(State(Customer(8_000)), [thrown]);

        var windUp = BusinessShopCombatChoreography.CreateMaomaoPose(
            resources.Catalog,
            snapshot,
            presentationFrame: 0,
            reduceMotion: false);
        var release = BusinessShopCombatChoreography.CreateMaomaoPose(
            resources.Catalog,
            snapshot,
            presentationFrame: 8,
            reduceMotion: false);
        var recovery = BusinessShopCombatChoreography.CreateMaomaoPose(
            resources.Catalog,
            snapshot,
            presentationFrame: 16,
            reduceMotion: false);

        Assert.Equal("maomao-wind-up", windUp.ClipId);
        Assert.Equal("maomao-throw", release.ClipId);
        Assert.Contains(release.Pose.Markers, marker => marker.Id == "release_product");
        Assert.Equal("maomao-recovery", recovery.ClipId);
    }

    [Fact]
    public async Task Choreography_KeepsServedAndEscapedCustomersVisibleForTheirExitAnimation()
    {
        using var resources = await Resources.LoadAsync();
        var served = Snapshot(State(), [new CustomerServedEvent(7, "regular")]);
        var escaped = Snapshot(State(), [new CustomerEscapedEvent(8, "impatient")]);

        var servedPoses = BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog, served, presentationFrame: 8, reduceMotion: false);
        var escapedPoses = BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog, escaped, presentationFrame: 8, reduceMotion: false);

        Assert.Collection(servedPoses, pose =>
        {
            Assert.Equal(7, pose.CustomerEntityId);
            Assert.Equal("regular", pose.ArchetypeId);
            Assert.Equal("customer-served", pose.ClipId);
            Assert.False(pose.ShowDemand);
        });
        Assert.Collection(escapedPoses, pose =>
        {
            Assert.Equal(8, pose.CustomerEntityId);
            Assert.Equal("impatient", pose.ArchetypeId);
            Assert.Equal("customer-leave", pose.ClipId);
            Assert.False(pose.ShowDemand);
        });
    }

    [Fact]
    public async Task Render_ProjectileAppearsOnlyWhenThrowReachesReleaseMarker()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        var customer = Customer(8_000);
        var projectile = new ProductProjectileState(
            2, "water", customer.EntityId, 6, 10, ["liquid"], ProductCombatEffectKind.None, 0, 6);
        var state = State(customer) with { Projectiles = [projectile] };
        var thrown = new ProductThrownEvent(2, "water", customer.EntityId);

        var before = renderer.Render(canvas, bitmap.Info, Frame(state, [thrown], animationFrame: 7));
        var released = renderer.Render(canvas, bitmap.Info, Frame(state, [thrown], animationFrame: 8));

        Assert.Equal(0, before.ProjectileDrawCount);
        Assert.Equal(1, released.ProjectileDrawCount);
    }

    [Fact]
    public void ProjectileRenderer_InterpolatesBetweenMaomaoAndTarget()
    {
        var projectile = new ProductProjectileState(
            2, "water", 1, 3, 10, ["liquid"], ProductCombatEffectKind.None, 0, 6);

        var point = ProductProjectileRenderer.Interpolate(
            projectile,
            originX: 80,
            originY: 120,
            targetX: 320,
            targetY: 140);

        Assert.Equal(200, point.X);
        Assert.True(point.Y < 130);
    }

    [Fact]
    public async Task Render_ProductHitProducesOneImpactFeedback()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        var hit = new ProductHitEvent(2, "water", 1, 25, 75, false);

        var stats = renderer.Render(canvas, bitmap.Info, Frame(State(Customer(5_000)), [hit]));

        Assert.Equal(1, stats.FeedbackDrawCount);
    }

    private static string RenderHash(CombatShopSceneRenderer renderer, StoreCombatState state)
    {
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        renderer.Render(canvas, bitmap.Info, Frame(state));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(data.ToArray()));
    }

    private static CombatShopSceneFrame Frame(
        StoreCombatState state,
        IReadOnlyList<CombatEvent>? events = null,
        long animationFrame = 6) =>
        new(
            Snapshot(state, events ?? []),
            Path.Combine(AppContext.BaseDirectory, "TestData", "interiors", "default-shop.png"),
            new Dictionary<string, string> { ["water"] = "product-beverage-water" },
            animationFrame,
            ReduceMotion: false);

    private static StoreCombatSnapshot Snapshot(
        StoreCombatState state,
        IReadOnlyList<CombatEvent>? events = null) =>
        new("corner-store", state, events ?? [], [], 0, 0, 0, 0);

    private static StoreCombatState State(params ActiveCustomerState[] customers) =>
        new(customers.Length + 1, 0, 0, customers, []);

    private static ActiveCustomerState Customer(int position) =>
        new(1, "regular", 100, position, 10, ["regular"], new Dictionary<string, int>(), 0, 0);

    private sealed class Resources : IDisposable
    {
        private Resources(SkeletalAnimationCatalog catalog, SKBitmap parts)
        {
            Catalog = catalog;
            Parts = parts;
        }

        public SkeletalAnimationCatalog Catalog { get; }
        public SKBitmap Parts { get; }

        public static async Task<Resources> LoadAsync()
        {
            var root = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
            var content = await new JsonCharacterAnimationCatalog(
                Path.Combine(root, "humanoid.json"),
                Path.Combine(root, "humanoid-clips.json"),
                Path.Combine(root, "skins.json")).LoadAsync();
            return new Resources(
                SkeletalAnimationCatalog.Create(content),
                SKBitmap.Decode(Path.Combine(root, "maomao-parts.png")));
        }

        public CombatShopSceneRenderer CreateRenderer() => new(Catalog, Parts);

        public void Dispose() => Parts.Dispose();
    }
}
