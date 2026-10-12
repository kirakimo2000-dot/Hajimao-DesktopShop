using System.Security.Cryptography;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Characters;
using HajimaoDesktopShop.Rendering.Combat;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests.Combat;

public sealed class CombatShopSceneRendererTests
{
    [Fact]
    public void FeedbackCopy_NamesSuccessfulServiceAndProductDrops()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "src",
            "HajimaoDesktopShop.Rendering",
            "Combat",
            "CombatShopSceneRenderer.cs"));

        Assert.Contains("招待成功", source, StringComparison.Ordinal);
        Assert.Contains("商品 +1", source, StringComparison.Ordinal);
        Assert.Contains("解锁新品！", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_NewProductUnlockDrawsASeparateCelebrationFeedback()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(840, 360, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        var events = new CombatEvent[]
        {
            new ProductHitEvent(5, "water", 1, 25, 0, false, 5_500),
            new CustomerServedEvent(1, "student")
        };
        var snapshot = new StoreCombatSnapshot(
            "corner-store",
            State(),
            events,
            [new ProductDropRoll(
                "first-service-guarantee",
                0,
                1,
                "chips",
                Awarded: true,
                IsNewUnlock: true)],
            190,
            1,
            0,
            1);
        var frame = new CombatShopSceneFrame(
            snapshot,
            Path.Combine(AppContext.BaseDirectory, "TestData", "interiors", "default-shop.png"),
            new Dictionary<string, string> { ["water"] = "product-beverage-water" },
            8,
            ReduceMotion: false);

        var stats = renderer.Render(canvas, bitmap.Info, frame);

        Assert.Equal(3, stats.FeedbackDrawCount);
        if (Environment.GetEnvironmentVariable("HAJIMAO_RENDER_SNAPSHOT_DIR") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(Path.Combine(output, "first-product-unlock-preview.png"));
            data.SaveTo(stream);
        }
    }

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
        Assert.Equal(1, stats.GroundShadowDrawCount);
    }

    [Fact]
    public async Task Render_GroundsEveryCharacterWithOnePixelShadow()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);

        var stats = renderer.Render(canvas, bitmap.Info, Frame(State(Customer(5_000, "student"))));

        Assert.Equal(2, stats.CharacterDrawCount);
        Assert.Equal(stats.CharacterDrawCount, stats.GroundShadowDrawCount);
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
    public async Task Render_CustomerArchetypesProduceDistinctGeometryNotJustDifferentTint()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();

        var studentGeometry = CustomerGeometryHash(renderer, "student");
        var officeGeometry = CustomerGeometryHash(renderer, "office-worker");
        var riderGeometry = CustomerGeometryHash(renderer, "delivery-rider");

        Assert.NotEqual(studentGeometry, officeGeometry);
        Assert.NotEqual(officeGeometry, riderGeometry);
        Assert.NotEqual(studentGeometry, riderGeometry);
    }

    [Fact]
    public async Task Choreography_ThrowUsesOneContinuousTwentyFourFrameClip()
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

        Assert.Equal("maomao-throw", windUp.ClipId);
        Assert.Equal("maomao-throw", release.ClipId);
        Assert.Contains(release.Pose.Markers, marker => marker.Id == "release_product");
        Assert.Equal("maomao-throw", recovery.ClipId);
        Assert.DoesNotContain(windUp.Pose.Markers, marker => marker.Id == "release_product");
        Assert.DoesNotContain(recovery.Pose.Markers, marker => marker.Id == "release_product");
    }

    [Fact]
    public void CustomerVisualProfiles_DifferentiateSilhouetteAndAccessory()
    {
        var student = HumanCharacterVisualCatalog.Resolve("student");
        var officeWorker = HumanCharacterVisualCatalog.Resolve("office-worker");
        var deliveryRider = HumanCharacterVisualCatalog.Resolve("delivery-rider");

        Assert.NotEqual(student.Accessory, officeWorker.Accessory);
        Assert.NotEqual(officeWorker.Accessory, deliveryRider.Accessory);
        Assert.NotEqual(student.ScaleX, officeWorker.ScaleX);
        Assert.NotEqual(officeWorker.AccentColor, deliveryRider.AccentColor);
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
    public async Task Choreography_StartsServedDepartureAtTheFatalImpactPosition()
    {
        using var resources = await Resources.LoadAsync();
        const int impactPosition = 8_000;
        var snapshot = Snapshot(State(),
        [
            new ProductHitEvent(2, "water", 7, 100, 0, false, impactPosition),
            new CustomerServedEvent(7, "student")
        ]);

        var pose = Assert.Single(BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog,
            snapshot,
            presentationFrame: 8,
            reduceMotion: false));

        Assert.Equal(BusinessShopCombatChoreography.CustomerX(impactPosition), pose.X);
    }

    [Fact]
    public async Task Choreography_OffsetsWalkCyclesSoCustomersDoNotMarchInLockstep()
    {
        using var resources = await Resources.LoadAsync();
        var first = Customer(7_000, "student");
        var second = first with { EntityId = 2, ArchetypeId = "office-worker" };
        var poses = BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog,
            Snapshot(State(first, second)),
            presentationFrame: 0,
            reduceMotion: false);

        Assert.Equal(2, poses.Count);
        Assert.NotEqual(poses[0].Pose.LogicalFrame, poses[1].Pose.LogicalFrame);
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
        Assert.Equal(122, point.Y);
    }

    [Fact]
    public void ProjectileRenderer_UsesSkeletalAnchorsAndDrawsAReadableTrail()
    {
        using var renderer = new ProductProjectileRenderer();
        using var bitmap = new SKBitmap(420, 180, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var customer = Customer(8_000);
        var projectile = new ProductProjectileState(
            2, "water", customer.EntityId, 3, 10, ["liquid"], ProductCombatEffectKind.None, 0, 6);
        var state = State(customer) with { Projectiles = [projectile] };
        var origin = new ProjectilePoint(40, 130);
        var target = new ProjectilePoint(300, 140);
        var point = ProductProjectileRenderer.Interpolate(
            projectile,
            origin.X,
            origin.Y,
            target.X,
            target.Y,
            simulationTickProgress: 0d);

        renderer.Draw(
            canvas,
            state,
            new Dictionary<string, string> { ["water"] = "product-beverage-water" },
            origin,
            new Dictionary<long, ProjectilePoint> { [customer.EntityId] = target });

        var hasTrailBeyondIcon = Enumerable.Range(Math.Max(0, point.X - 16), 8)
            .Any(x => Enumerable.Range(Math.Max(0, point.Y - 5), 11)
                .Any(y => bitmap.GetPixel(x, y).Alpha > 0));
        Assert.True(hasTrailBeyondIcon);
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

    [Fact]
    public async Task Render_ProductHitFeedbackUsesTheRenderedHumanHeadAnchor()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        var customer = Customer(8_000, "student");
        var hit = new ProductHitEvent(2, "water", customer.EntityId, 25, 75, false, customer.PositionPermille);
        var snapshot = Snapshot(State(customer), [hit]);
        var customerPose = Assert.Single(BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog,
            snapshot,
            presentationFrame: 6,
            reduceMotion: false,
            simulationTickProgress: 6d / 23d));
        var visual = HumanCharacterVisualCatalog.Resolve(customer.ArchetypeId);
        var head = HumanCharacterRenderer.HeadAnchor(
            customerPose.Pose,
            visual,
            customerPose.X,
            customerPose.Y,
            facingLeft: true);

        renderer.Render(canvas, bitmap.Info, Frame(State(customer), [hit]));

        var hitColor = SKColor.Parse("#E15A5A");
        var feedbackNearHead = Enumerable.Range((int)head.X - 16, 33)
            .Any(x => Enumerable.Range((int)head.Y - 28, 24)
                .Any(y => bitmap.GetPixel(x, y) == hitColor));
        Assert.True(feedbackNearHead);
    }

    [Theory]
    [InlineData("student")]
    [InlineData("collector")]
    public async Task Render_DemandBarSitsImmediatelyAboveEachRenderedHead(string archetypeId)
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        var customer = Customer(8_000, archetypeId);
        var snapshot = Snapshot(State(customer));
        var pose = Assert.Single(BusinessShopCombatChoreography.CreateCustomerPoses(
            resources.Catalog,
            snapshot,
            presentationFrame: 6,
            reduceMotion: false,
            simulationTickProgress: 6d / 23d));
        var visual = HumanCharacterVisualCatalog.Resolve(archetypeId);
        var head = HumanCharacterRenderer.HeadAnchor(
            pose.Pose,
            visual,
            pose.X,
            pose.Y,
            facingLeft: true);

        renderer.Render(canvas, bitmap.Info, Frame(State(customer), animationFrame: 6));

        Assert.Equal(
            SKColor.Parse("#302F36"),
            bitmap.GetPixel((int)MathF.Round(head.X), (int)MathF.Round(head.Y) - 6));
        Assert.NotEqual(
            SKColor.Parse("#302F36"),
            bitmap.GetPixel((int)MathF.Round(head.X), (int)MathF.Round(head.Y) - 8));
    }

    [Fact]
    public async Task Render_DamageTextRisesRelativeToTheMovingHead()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        var customer = Customer(8_000, "student");
        var hit = new ProductHitEvent(2, "water", customer.EntityId, 25, 75, false, customer.PositionPermille);

        var initial = RenderDamageFrame(renderer, resources.Catalog, customer, hit, animationFrame: 0);
        var later = RenderDamageFrame(renderer, resources.Catalog, customer, hit, animationFrame: 12);

        Assert.True(later.FeedbackTopRelativeToHead < initial.FeedbackTopRelativeToHead);
    }

    [Fact]
    public async Task Render_DamageTextHasNoSolidUnderline()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        var customer = Customer(8_000, "student");
        var hit = new ProductHitEvent(2, "water", customer.EntityId, 25, 75, false, customer.PositionPermille);

        var frame = RenderDamageFrame(renderer, resources.Catalog, customer, hit, animationFrame: 6);

        Assert.InRange(frame.MaximumFeedbackRun, 1, 8);
    }

    [Fact]
    public async Task Render_DamageTextStaysAboveTheDemandBar()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        var customer = Customer(8_000, "student");
        var hit = new ProductHitEvent(2, "water", customer.EntityId, 25, 75, false, customer.PositionPermille);

        var frame = RenderDamageFrame(renderer, resources.Catalog, customer, hit, animationFrame: 8);

        Assert.True(frame.FeedbackBottom < frame.DemandBarTop);
    }

    [Fact]
    public async Task Render_FatalHitFeedbackUsesStoredImpactPositionAfterCustomerLeavesState()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        const int impactPosition = 8_000;
        var events = new CombatEvent[]
        {
            new ProductHitEvent(2, "water", 1, 100, 0, false, impactPosition),
            new CustomerServedEvent(1, "student")
        };

        renderer.Render(canvas, bitmap.Info, Frame(State(), events));

        var expectedX = BusinessShopCombatChoreography.CustomerX(impactPosition);
        var feedbackColor = SKColor.Parse("#F1B844");
        var feedbackAtImpact = Enumerable.Range(expectedX - 16, 33)
            .Any(x => Enumerable.Range(80, 42)
                .Any(y => bitmap.GetPixel(x, y) == feedbackColor));
        Assert.True(feedbackAtImpact);
    }

    [Fact]
    public async Task Render_RepresentativeCharacterSceneKeepsEveryLayerVisible()
    {
        using var resources = await Resources.LoadAsync();
        using var renderer = resources.CreateRenderer();
        using var bitmap = new SKBitmap(840, 360, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        var student = Customer(3_000, "student");
        var officeWorker = Customer(5_500, "office-worker") with { EntityId = 2 };
        var deliveryRider = Customer(8_000, "delivery-rider") with { EntityId = 3 };
        var projectile = new ProductProjectileState(
            4, "water", deliveryRider.EntityId, 1, 10, ["liquid"], ProductCombatEffectKind.None, 0, 1);
        var state = State(student, officeWorker, deliveryRider) with { Projectiles = [projectile] };
        var events = new CombatEvent[]
        {
            new ProductThrownEvent(projectile.EntityId, projectile.ProductId, deliveryRider.EntityId),
            new ProductHitEvent(5, "water", officeWorker.EntityId, 25, 75, false)
        };

        var stats = renderer.Render(canvas, bitmap.Info, Frame(state, events, animationFrame: 8));

        Assert.Equal(4, stats.CharacterDrawCount);
        Assert.Equal(4, stats.GroundShadowDrawCount);
        Assert.Equal(1, stats.ProjectileDrawCount);
        Assert.Equal(1, stats.FeedbackDrawCount);
        if (Environment.GetEnvironmentVariable("HAJIMAO_RENDER_SNAPSHOT_DIR") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(Path.Combine(output, "character-combat-preview.png"));
            data.SaveTo(stream);

            using var sheet = new SKBitmap(420 * 6, 180 * 4, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var sheetCanvas = new SKCanvas(sheet);
            for (var animationFrame = 0; animationFrame < 24; animationFrame++)
            {
                using var frameBitmap = new SKBitmap(420, 180, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var frameCanvas = new SKCanvas(frameBitmap);
                renderer.Render(frameCanvas, frameBitmap.Info, Frame(state, events, animationFrame));
                using var frameImage = SKImage.FromBitmap(frameBitmap);
                var left = (animationFrame % 6) * 420;
                var top = (animationFrame / 6) * 180;
                sheetCanvas.DrawImage(
                    frameImage,
                    new SKRect(0, 0, 420, 180),
                    new SKRect(left, top, left + 420, top + 180),
                    new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
                    paint: null);
            }

            using var sheetImage = SKImage.FromBitmap(sheet);
            using var sheetData = sheetImage.Encode(SKEncodedImageFormat.Png, 100);
            using var sheetStream = File.Create(Path.Combine(output, "character-combat-24-frame-sheet.png"));
            sheetData.SaveTo(sheetStream);
        }
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

    private static DamageFrameStats RenderDamageFrame(
        CombatShopSceneRenderer renderer,
        SkeletalAnimationCatalog catalog,
        ActiveCustomerState customer,
        ProductHitEvent hit,
        long animationFrame)
    {
        using var bitmap = new SKBitmap(420, 180);
        using var canvas = new SKCanvas(bitmap);
        var snapshot = Snapshot(State(customer), [hit]);
        var pose = Assert.Single(BusinessShopCombatChoreography.CreateCustomerPoses(
            catalog,
            snapshot,
            animationFrame,
            reduceMotion: false,
            simulationTickProgress: animationFrame / 23d));
        var visual = HumanCharacterVisualCatalog.Resolve(customer.ArchetypeId);
        var head = HumanCharacterRenderer.HeadAnchor(
            pose.Pose,
            visual,
            pose.X,
            pose.Y,
            facingLeft: true);
        renderer.Render(canvas, bitmap.Info, Frame(State(customer), [hit], animationFrame));

        var color = SKColor.Parse("#E15A5A");
        var demandBorder = SKColor.Parse("#302F36");
        var top = bitmap.Height;
        var bottom = 0;
        var demandTop = bitmap.Height;
        var maximumRun = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            var run = 0;
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                {
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                    run++;
                    maximumRun = Math.Max(maximumRun, run);
                }
                else
                {
                    run = 0;
                }

                if (Math.Abs(x - (int)MathF.Round(head.X)) <= 20
                    && bitmap.GetPixel(x, y) == demandBorder)
                {
                    demandTop = Math.Min(demandTop, y);
                }
            }
        }

        return new DamageFrameStats(
            top - (int)MathF.Round(head.Y),
            maximumRun,
            bottom,
            demandTop);
    }

    private static string CustomerGeometryHash(CombatShopSceneRenderer renderer, string archetypeId)
    {
        using var baseline = new SKBitmap(420, 180);
        using var baselineCanvas = new SKCanvas(baseline);
        renderer.Render(baselineCanvas, baseline.Info, Frame(State()));
        using var candidate = new SKBitmap(420, 180);
        using var candidateCanvas = new SKCanvas(candidate);
        renderer.Render(candidateCanvas, candidate.Info, Frame(State(Customer(5_000, archetypeId))));
        var geometry = new byte[baseline.Width * baseline.Height];
        var offset = 0;
        for (var y = 0; y < baseline.Height; y++)
        {
            for (var x = 0; x < baseline.Width; x++)
            {
                geometry[offset++] = baseline.GetPixel(x, y) == candidate.GetPixel(x, y)
                    ? (byte)0
                    : (byte)1;
            }
        }

        return Convert.ToHexString(SHA256.HashData(geometry));
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

    private static ActiveCustomerState Customer(int position, string archetypeId = "regular") =>
        new(1, archetypeId, 100, position, 10, ["regular"], new Dictionary<string, int>(), 0, 0);

    private sealed record DamageFrameStats(
        int FeedbackTopRelativeToHead,
        int MaximumFeedbackRun,
        int FeedbackBottom,
        int DemandBarTop);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory);
    }

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
