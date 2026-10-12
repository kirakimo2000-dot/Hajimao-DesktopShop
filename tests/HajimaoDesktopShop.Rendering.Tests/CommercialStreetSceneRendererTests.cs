using System.Security.Cryptography;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Tests;

public sealed class CommercialStreetSceneRendererTests
{
    [Fact]
    public void Renderer_DrawsOpenedStorefrontVehicleAndRainWithoutMutatingSnapshot()
    {
        var snapshot = CreateSnapshot(StreetWeather.Rain, pedestrians: 3, vehicles: 1);
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(snapshot.Stores.Count),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var resources = CharacterResources.Load();
        using var renderer = resources.CreateRenderer();

        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

        Assert.Equal(SKColor.Parse("#65B8C8"), bitmap.GetPixel(8, 8));
        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(15, 50));
        Assert.Equal(SKColor.Parse("#E15A5A"), bitmap.GetPixel(50, 165));
        Assert.Equal(3, snapshot.VisiblePedestrians);
        Assert.Equal(1, snapshot.VisibleVehicles);
        if (Environment.GetEnvironmentVariable("HAJIMAO_RENDER_SNAPSHOT_DIR") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(Path.Combine(output, "street-human-preview.png"));
            data.SaveTo(stream);
        }
    }

    [Fact]
    public void Renderer_UsesAnimatedPedestrianFramesAndHonorsReducedMotion()
    {
        var seed = RenderHash(animationFrame: 0, reduceMotion: false);
        var moving = RenderHash(animationFrame: 2, reduceMotion: false);
        var reducedSeed = RenderHash(animationFrame: 0, reduceMotion: true);
        var reduced = RenderHash(animationFrame: 2, reduceMotion: true);

        Assert.NotEqual(seed, moving);
        Assert.Equal(reducedSeed, reduced);
    }

    [Fact]
    public async Task Renderer_StreetPedestriansUseTheSharedHumanoidParts()
    {
        using var resources = await CharacterResources.LoadAsync();
        using var emptyParts = new SKBitmap(
            resources.Parts.Width,
            resources.Parts.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        emptyParts.Erase(SKColors.Transparent);
        using var sharedRenderer = new CommercialStreetSceneRenderer(resources.Catalog, resources.Parts);
        using var emptyRenderer = new CommercialStreetSceneRenderer(resources.Catalog, emptyParts);
        var snapshot = CreateSnapshot(StreetWeather.Clear, pedestrians: 1, vehicles: 0);

        var shared = RenderHash(sharedRenderer, snapshot);
        var empty = RenderHash(emptyRenderer, snapshot);

        Assert.NotEqual(shared, empty);
    }

    [Fact]
    public void Renderer_MovesPedestriansAfterACompleteTwentyFourFrameAnimationCycle()
    {
        var origin = RenderHash(animationFrame: 0, reduceMotion: false);
        var advanced = RenderHash(animationFrame: 24, reduceMotion: false);

        Assert.NotEqual(origin, advanced);
    }

    [Fact]
    public void Renderer_CapsVisibleStreetActorsToSceneBudget()
    {
        var capped = RenderHash(0, false, pedestrians: 6, vehicles: 2);
        var overflow = RenderHash(0, false, pedestrians: 99, vehicles: 99);

        Assert.Equal(capped, overflow);
    }

    [Fact]
    public void Renderer_AcceptsOpenedStoresBeyondTheOldTierSlotCap()
    {
        var stores = Enumerable.Range(0, 12)
            .Select(index => new CommercialStreetStoreSnapshot(
                $"store-{index}",
                $"店铺 {index}",
                8_000,
                10_000 / 12))
            .ToArray();
        var snapshot = new CommercialStreetSnapshot(
            CommercialStreetTier.Corner,
            StreetWeather.Clear,
            8_000,
            0,
            0,
            stores);
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(stores.Length),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var resources = CharacterResources.Load();
        using var renderer = resources.CreateRenderer();

        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

        var last = CommercialStreetLayout.CreateStorefronts(stores)[^1].Bounds;
        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(last.X + 4, last.Y + 20));
    }

    [Fact]
    public void Renderer_CameraOffsetMovesContentInsideAClampedViewport()
    {
        var snapshot = CreateSnapshot(StreetWeather.Clear, pedestrians: 0, vehicles: 0);
        using var bitmap = new SKBitmap(248, CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var resources = CharacterResources.Load();
        using var renderer = resources.CreateRenderer();

        renderer.Render(
            canvas,
            bitmap.Info,
            new CommercialStreetSceneFrame(snapshot, CameraOffset: 232));

        Assert.Equal(SKColor.Parse("#6B4634"), bitmap.GetPixel(16, 48));
    }

    [Fact]
    public void Renderer_UsesFacadeStyleKeyForVisibleStoreVariation()
    {
        var convenience = RenderFacade("facade-convenience-a");
        var premium = RenderFacade("facade-premium-d");

        Assert.NotEqual(convenience, premium);
    }

    [Fact]
    public void Renderer_DrawsDistinctGeometryForEveryStoreFormat()
    {
        var formats = new[]
        {
            "convenience", "commuter", "discount", "warehouse",
            "lifestyle", "health", "premium", "variety"
        };
        var hashes = formats.Select(format => RenderFacade($"facade-{format}-a")).ToArray();

        Assert.Equal(hashes.Length, hashes.Distinct(StringComparer.Ordinal).Count());
        if (Environment.GetEnvironmentVariable("HAJIMAO_RENDER_SNAPSHOT_DIR") is { Length: > 0 } output)
        {
            var stores = formats.Select((format, index) => new CommercialStreetStoreSnapshot(
                $"store-{index}",
                format,
                8_000,
                10_000 / formats.Length,
                $"facade-{format}-a",
                IsStaffed: index != 5)).ToArray();
            var snapshot = new CommercialStreetSnapshot(
                CommercialStreetTier.Block,
                StreetWeather.Clear,
                8_000,
                0,
                0,
                stores);
            using var bitmap = new SKBitmap(
                CommercialStreetLayout.GetContentWidth(stores.Length),
                CommercialStreetLayout.LogicalHeight);
            using var canvas = new SKCanvas(bitmap);
            using var resources = CharacterResources.Load();
            using var renderer = resources.CreateRenderer();
            renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));
            Directory.CreateDirectory(output);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(Path.Combine(output, "street-facade-preview.png"));
            data.SaveTo(stream);
        }
    }

    [Fact]
    public void Renderer_DrawsAClosedShutterOnAnUnstaffedStore()
    {
        var staffed = RenderFacade("facade-convenience-a", isStaffed: true);
        var unstaffed = RenderFacade("facade-convenience-a", isStaffed: false);

        Assert.NotEqual(staffed, unstaffed);
    }

    [Fact]
    public void Renderer_UsesTheFacadeTextureInsteadOfTheProceduralPlaceholder()
    {
        using var resources = CharacterResources.Load();
        using var texture = new SKBitmap(224, 102, SKColorType.Rgba8888, SKAlphaType.Premul);
        texture.Erase(SKColor.Parse("#D946EF"));
        var textures = Enum.GetValues<FacadeGeometryKind>()
            .ToDictionary(kind => kind, _ => texture);
        using var renderer = new CommercialStreetSceneRenderer(
            resources.Catalog,
            resources.Parts,
            textures);
        var snapshot = new CommercialStreetSnapshot(
            CommercialStreetTier.Corner,
            StreetWeather.Clear,
            0,
            0,
            0,
            [new CommercialStreetStoreSnapshot(
                "corner", "街角店", 8_000, 10_000, "facade-convenience-a")]);
        using var bitmap = new SKBitmap(248, CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);

        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

        Assert.Equal(SKColor.Parse("#D946EF"), bitmap.GetPixel(100, 70));
    }

    [Fact]
    public void Renderer_DrawsAllEightShippedStorefrontTexturesInOneStreet()
    {
        var formats = Enum.GetValues<FacadeGeometryKind>();
        var textureRoot = Path.Combine(AppContext.BaseDirectory, "TestData", "facades");
        var textures = StorefrontTextureSet.Load(textureRoot);
        try
        {
            using var resources = CharacterResources.Load();
            using var renderer = new CommercialStreetSceneRenderer(
                resources.Catalog,
                resources.Parts,
                textures);
            var stores = formats.Select((kind, index) => new CommercialStreetStoreSnapshot(
                $"store-{index}",
                kind.ToString(),
                8_000,
                1_250,
                $"facade-{kind.ToString().ToLowerInvariant()}-a")).ToArray();
            var snapshot = new CommercialStreetSnapshot(
                CommercialStreetTier.Block,
                StreetWeather.Clear,
                8_000,
                0,
                0,
                stores);
            using var bitmap = new SKBitmap(
                CommercialStreetLayout.GetContentWidth(stores.Length),
                CommercialStreetLayout.LogicalHeight);
            using var canvas = new SKCanvas(bitmap);

            renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));

            Assert.NotEqual(SKColor.Parse("#31262A"), bitmap.GetPixel(124, 80));
            if (Environment.GetEnvironmentVariable("HAJIMAO_RENDER_SNAPSHOT_DIR") is { Length: > 0 } output)
            {
                Directory.CreateDirectory(output);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(Path.Combine(output, "street-texture-preview.png"));
                data.SaveTo(stream);
            }
        }
        finally
        {
            foreach (var texture in textures.Values)
            {
                texture.Dispose();
            }
        }
    }

    private static string RenderFacade(string facadeStyleKey, bool isStaffed = true)
    {
        var snapshot = new CommercialStreetSnapshot(
            CommercialStreetTier.Corner,
            StreetWeather.Clear,
            8_000,
            0,
            0,
            [new CommercialStreetStoreSnapshot("corner", "街角店", 8_000, 10_000, facadeStyleKey, isStaffed)]);
        using var bitmap = new SKBitmap(248, CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var resources = CharacterResources.Load();
        using var renderer = resources.CreateRenderer();
        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }

    private static string RenderHash(
        CommercialStreetSceneRenderer renderer,
        CommercialStreetSnapshot snapshot)
    {
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(snapshot.Stores.Count),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        renderer.Render(canvas, bitmap.Info, new CommercialStreetSceneFrame(snapshot));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }

    private static string RenderHash(
        int animationFrame,
        bool reduceMotion,
        int pedestrians = 3,
        int vehicles = 1)
    {
        using var bitmap = new SKBitmap(
            CommercialStreetLayout.GetContentWidth(2),
            CommercialStreetLayout.LogicalHeight);
        using var canvas = new SKCanvas(bitmap);
        using var resources = CharacterResources.Load();
        using var renderer = resources.CreateRenderer();
        renderer.Render(
            canvas,
            bitmap.Info,
            new CommercialStreetSceneFrame(
                CreateSnapshot(StreetWeather.Clear, pedestrians, vehicles),
                animationFrame,
                reduceMotion));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToHexString(SHA256.HashData(encoded.ToArray()));
    }

    private static CommercialStreetSnapshot CreateSnapshot(
        StreetWeather weather,
        int pedestrians,
        int vehicles) =>
        new(
            CommercialStreetTier.Street,
            weather,
            8_000,
            pedestrians,
            vehicles,
            [
                new CommercialStreetStoreSnapshot("corner", "街角店", 8_000, 6_000),
                new CommercialStreetStoreSnapshot("station", "车站店", 5_000, 4_000)
            ]);

    private sealed class CharacterResources : IDisposable
    {
        private CharacterResources(SkeletalAnimationCatalog catalog, SKBitmap parts)
        {
            Catalog = catalog;
            Parts = parts;
        }

        public SkeletalAnimationCatalog Catalog { get; }
        public SKBitmap Parts { get; }

        public static async Task<CharacterResources> LoadAsync()
        {
            var root = Path.Combine(AppContext.BaseDirectory, "TestData", "characters");
            var content = await new JsonCharacterAnimationCatalog(
                Path.Combine(root, "humanoid.json"),
                Path.Combine(root, "humanoid-clips.json"),
                Path.Combine(root, "skins.json")).LoadAsync();
            return new CharacterResources(
                SkeletalAnimationCatalog.Create(content),
                SKBitmap.Decode(Path.Combine(root, "maomao-parts.png")));
        }

        public static CharacterResources Load() =>
            LoadAsync().GetAwaiter().GetResult();

        public CommercialStreetSceneRenderer CreateRenderer() =>
            new(Catalog, Parts);

        public void Dispose() => Parts.Dispose();
    }
}
