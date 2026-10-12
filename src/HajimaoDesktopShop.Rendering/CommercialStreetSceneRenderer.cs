using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Characters;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class CommercialStreetSceneRenderer : IDisposable
{
    public const int LogicalHeight = CommercialStreetLayout.LogicalHeight;

    private static readonly string[] FacadeBodies =
        ["#6B4634", "#4B5964", "#735244", "#48515C", "#8A765D", "#4D6A62", "#594453", "#7B554B"];

    private static readonly string[] FacadeSigns =
        ["#F1B844", "#65B8C8", "#E15A5A", "#A6ABB4", "#F4EBDD", "#72C986", "#D4AE67", "#E8905B"];

    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };
    private readonly SkeletalAnimationCatalog _characters;
    private readonly SKImage _parts;
    private readonly IReadOnlyDictionary<FacadeGeometryKind, SKBitmap> _storefrontTextures;

    public CommercialStreetSceneRenderer(
        SkeletalAnimationCatalog characters,
        SKBitmap parts,
        IReadOnlyDictionary<FacadeGeometryKind, SKBitmap>? storefrontTextures = null)
    {
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        ArgumentNullException.ThrowIfNull(parts);
        _parts = SKImage.FromBitmap(parts);
        _storefrontTextures = (storefrontTextures ?? new Dictionary<FacadeGeometryKind, SKBitmap>())
            .ToDictionary(entry => entry.Key, entry => entry.Value.Copy());
    }

    public void Render(SKCanvas canvas, SKImageInfo target, CommercialStreetSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        canvas.Clear(SKColor.Parse("#17191D"));
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var contentWidth = CommercialStreetLayout.GetContentWidth(frame.Snapshot.Stores.Count);
        var scale = Math.Max(1, target.Height / LogicalHeight);
        var viewportWidth = Math.Min(contentWidth, Math.Max(1, target.Width / scale));
        var cameraOffset = CommercialStreetLayout.ClampCameraOffset(
            contentWidth,
            viewportWidth,
            frame.CameraOffset);
        canvas.Save();
        canvas.Translate(
            (target.Width - viewportWidth * scale) / 2,
            (target.Height - LogicalHeight * scale) / 2);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, viewportWidth, LogicalHeight));
        canvas.Translate(-cameraOffset, 0);
        DrawLogicalScene(canvas, frame);
        canvas.Restore();
    }

    public void DrawLogicalScene(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        var street = frame.Snapshot;
        var contentWidth = CommercialStreetLayout.GetContentWidth(street.Stores.Count);
        Fill(canvas, 0, 0, contentWidth, 28, SkyColor(street.Weather));
        Fill(canvas, 0, 28, contentWidth, 102, "#31262A");
        Fill(canvas, 0, 130, contentWidth, 20, "#B87349");
        Fill(canvas, 0, 150, contentWidth, 30, "#34383F");
        Fill(canvas, 0, 152, contentWidth, 3, "#F1B844");
        Fill(canvas, 0, 174, contentWidth, 3, "#D9D2C3");

        DrawStorefronts(canvas, street);
        DrawPedestrians(canvas, frame);
        DrawVehicles(canvas, street.VisibleVehicles, contentWidth, frame);
        DrawWeather(canvas, street.Weather, contentWidth);
    }

    public void Dispose()
    {
        _parts.Dispose();
        foreach (var texture in _storefrontTextures.Values)
        {
            texture.Dispose();
        }
        _paint.Dispose();
    }

    private void DrawStorefronts(SKCanvas canvas, CommercialStreetSnapshot street)
    {
        var storefronts = CommercialStreetLayout.CreateStorefronts(street.Stores);
        for (var index = 0; index < storefronts.Count; index++)
        {
            var bounds = storefronts[index].Bounds;
            FacadeSpriteVariant variant;
            try
            {
                variant = ContentSpriteKey.ResolveFacade(street.Stores[index].FacadeStyleKey);
            }
            catch (ArgumentException)
            {
                variant = new FacadeSpriteVariant(0, 0);
            }

            if (_storefrontTextures.TryGetValue(variant.GeometryKind, out var texture))
            {
                canvas.DrawBitmap(
                    texture,
                    bounds.X,
                    bounds.Y,
                    new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
                    null);
            }
            else
            {
                DrawFacade(canvas, bounds, variant);
            }
            if (!street.Stores[index].IsStaffed)
            {
                DrawClosedStorefront(canvas, bounds);
            }
        }
    }

    private void DrawClosedStorefront(SKCanvas canvas, LogicalPixelRect bounds)
    {
        Fill(canvas, bounds.X + 10, bounds.Bottom - 54, bounds.Width - 20, 46, "#3A3D43");
        for (var y = bounds.Bottom - 50; y < bounds.Bottom - 10; y += 7)
        {
            Fill(canvas, bounds.X + 12, y, bounds.Width - 24, 2, "#666A73");
        }

        Fill(canvas, bounds.X + bounds.Width / 2 - 16, bounds.Bottom - 35, 32, 11, "#24262B");
        Fill(canvas, bounds.X + bounds.Width / 2 - 2, bounds.Bottom - 31, 4, 4, "#E15A5A");
    }

    private void DrawFacade(SKCanvas canvas, LogicalPixelRect bounds, FacadeSpriteVariant variant)
    {
        var geometry = FacadeGeometryPolicy.Resolve(variant.GeometryKind);
        var body = FacadeBodies[variant.PaletteIndex];
        var accent = FacadeSigns[variant.PaletteIndex];
        var x = bounds.X + geometry.SideInset;
        var y = bounds.Y + geometry.TopOffset;
        var width = bounds.Width - geometry.SideInset * 2;
        var height = bounds.Bottom - y;

        Fill(canvas, x, y, width, height, body);
        if (geometry.HasCornice)
        {
            Fill(canvas, x - 4, y, width + 8, 5, "#29282E");
            Fill(canvas, x, y + 5, width, 3, accent);
        }

        if (variant.GeometryKind == FacadeGeometryKind.Warehouse)
        {
            for (var roofX = x; roofX < x + width; roofX += 28)
            {
                Fill(canvas, roofX, y - 4, Math.Min(18, x + width - roofX), 4, accent);
            }
        }
        else if (variant.GeometryKind == FacadeGeometryKind.Premium)
        {
            Fill(canvas, x + 8, y - 5, width - 16, 5, accent);
            Fill(canvas, x + width / 2 - 3, y - 10, 6, 5, accent);
        }
        else if (variant.GeometryKind == FacadeGeometryKind.Lifestyle)
        {
            Fill(canvas, x + width / 2, y - 6, width / 2, 6, body);
        }

        var signX = x + 8;
        var signY = y + geometry.SignTop;
        var signWidth = width - 16;
        if (geometry.HasVerticalSign)
        {
            var verticalWidth = 13 + variant.AwningIndex;
            signX = geometry.DoorOnRight ? x + 7 : x + width - verticalWidth - 7;
            signWidth = verticalWidth;
            Fill(canvas, signX, signY, signWidth, 32, accent);
        }
        else
        {
            Fill(canvas, signX, signY, signWidth, geometry.SignHeight, accent);
        }

        var shopTop = y + Math.Max(35, geometry.SignTop + geometry.SignHeight + 7);
        var shopBottom = bounds.Bottom - 8;
        var shopHeight = Math.Max(12, shopBottom - shopTop);
        var gap = 4;
        var bayWidth = Math.Max(12, (width - 20 - gap * (geometry.WindowBayCount - 1)) / geometry.WindowBayCount);
        for (var bay = 0; bay < geometry.WindowBayCount; bay++)
        {
            var bayX = x + 10 + bay * (bayWidth + gap);
            var isDoor = geometry.DoorOnRight
                ? bay == geometry.WindowBayCount - 1
                : bay == 0;
            Fill(canvas, bayX, shopTop, bayWidth, shopHeight, isDoor ? "#4A353C" : "#65B8C8");
            if (!isDoor)
            {
                Fill(canvas, bayX + bayWidth / 2, shopTop, 2, shopHeight, "#DCE7E5");
            }
        }

        DrawAwning(
            canvas,
            new LogicalPixelRect(x, shopTop - 5, width, height),
            variant.AwningIndex,
            accent);
    }

    private void DrawAwning(
        SKCanvas canvas,
        LogicalPixelRect bounds,
        int style,
        string color)
    {
        var stripeWidth = 8 + style * 2;
        for (var x = bounds.X + 10; x < bounds.X + bounds.Width - 10; x += stripeWidth * 2)
        {
            Fill(canvas, x, bounds.Y, stripeWidth, 4 + style, color);
        }
    }

    private void DrawPedestrians(SKCanvas canvas, CommercialStreetSceneFrame frame)
    {
        var count = Math.Min(
            Math.Max(0, frame.Snapshot.VisiblePedestrians),
            PixelArtBudget.MaximumVisibleStreetPedestrians);
        var rig = _characters.Rigs["humanoid-v1"];
        var skin = _characters.Skins["maomao-default"];
        var clip = _characters.Clips["customer-walk"];
        var contentWidth = CommercialStreetLayout.GetContentWidth(frame.Snapshot.Stores.Count);
        for (var index = 0; index < count; index++)
        {
            var anchorX = CharacterMotion.HorizontalLoop(
                frame.AnimationFrame,
                index * 17,
                24,
                contentWidth - 24,
                4,
                frame.ReduceMotion);
            var archetypeId = HumanCharacterVisualCatalog.ResolveStreetArchetype(index);
            var visual = HumanCharacterVisualCatalog.Resolve(archetypeId);
            var pose = SkeletalAnimator.Evaluate(
                rig,
                clip,
                frame.AnimationFrame + index * 7L,
                frame.ReduceMotion);
            DrawPedestrianShadow(canvas, anchorX, 150, visual.ScaleX);
            HumanCharacterRenderer.Draw(
                canvas,
                _parts,
                _paint,
                rig,
                skin,
                pose,
                visual,
                anchorX,
                143,
                facingLeft: false);
        }
    }

    private void DrawPedestrianShadow(SKCanvas canvas, int x, int y, float scaleX)
    {
        _paint.Color = SKColor.Parse("#242229").WithAlpha(190);
        var halfWidth = Math.Max(6, (int)MathF.Round(9 * scaleX));
        canvas.DrawRect(x - halfWidth, y - 1, halfWidth * 2, 3, _paint);
    }

    private void DrawVehicles(
        SKCanvas canvas,
        int requestedCount,
        int contentWidth,
        CommercialStreetSceneFrame frame)
    {
        var count = Math.Min(Math.Max(0, requestedCount), PixelArtBudget.MaximumVisibleStreetVehicles);
        for (var index = 0; index < count; index++)
        {
            var x = CharacterMotion.HorizontalLoop(
                frame.AnimationFrame,
                10 + index * 53,
                0,
                contentWidth - 56,
                4,
                frame.ReduceMotion);
            Fill(canvas, x, 158, 54, 15, index == 0 ? "#E15A5A" : "#65B8C8");
            Fill(canvas, x + 10, 154, 30, 7, "#A6ABB4");
            Fill(canvas, x + 7, 171, 9, 5, "#17191D");
            Fill(canvas, x + 39, 171, 9, 5, "#17191D");
        }
    }

    private void DrawWeather(SKCanvas canvas, StreetWeather weather, int contentWidth)
    {
        if (weather == StreetWeather.Rain)
        {
            for (var x = 8; x < contentWidth; x += 24)
            {
                Fill(canvas, x, 4 + (x / 24) % 3 * 10, 2, 7, "#65B8C8");
            }
        }
        else if (weather == StreetWeather.Wind)
        {
            for (var x = 28; x < contentWidth; x += 78)
            {
                Fill(canvas, x, 14 + (x / 78) % 2 * 9, 28, 2, "#A6ABB4");
            }
        }
    }

    private static string SkyColor(StreetWeather weather) => weather switch
    {
        StreetWeather.Clear => "#506878",
        StreetWeather.Cloudy => "#46515C",
        StreetWeather.Rain => "#31404B",
        StreetWeather.Wind => "#3F5664",
        _ => "#506878"
    };

    private void Fill(SKCanvas canvas, int x, int y, int width, int height, string color)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }
}
