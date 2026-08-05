using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class BusinessShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;

    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    private static readonly IReadOnlyDictionary<string, int> ProductFrameIndexes =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["water"] = 0,
            ["bread"] = 1,
            ["instant_noodles"] = 2,
            ["chips"] = 3,
            ["milk"] = 4,
            ["soda"] = 5,
            ["sandwich"] = 6,
            ["yogurt"] = 7,
            ["ice_cream"] = 8,
            ["dumplings"] = 9
        };

    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };
    private readonly PixelSpriteAtlas _atlas;
    private readonly SKImage _atlasImage;

    public BusinessShopSceneRenderer()
    {
        _atlas = PixelSpriteAtlas.LoadDefault();
        _atlasImage = SKImage.FromBitmap(_atlas.Bitmap);
    }

    public void Render(SKCanvas canvas, SKImageInfo target, BusinessShopSceneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(frame);
        canvas.Clear(SKColor.Parse("#17191D"));
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var scale = Math.Max(1, Math.Min(target.Width / LogicalWidth, target.Height / LogicalHeight));
        canvas.Save();
        canvas.Translate(
            (target.Width - LogicalWidth * scale) / 2,
            (target.Height - LogicalHeight * scale) / 2);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, LogicalWidth, LogicalHeight));
        DrawLogicalScene(canvas, frame);
        canvas.Restore();
    }

    public void DrawLogicalScene(SKCanvas canvas, BusinessShopSceneFrame frame)
    {
        var store = frame.Snapshot.Business.Stores.SingleOrDefault(item => item.Id == frame.StoreId);
        var operations = frame.Snapshot.Stores.SingleOrDefault(item => item.StoreId == frame.StoreId);
        var employees = frame.Snapshot.Employees.Employees
            .Where(employee => employee.StoreId == frame.StoreId)
            .ToArray();

        Fill(canvas, 0, 0, LogicalWidth, LogicalHeight, "#17191D");
        Fill(canvas, 0, 8, LogicalWidth, 116, "#4A353C");
        Fill(canvas, 0, 124, LogicalWidth, 56, "#B87349");
        Fill(canvas, 0, 120, LogicalWidth, 4, "#31262A");
        DrawSprite(canvas, PixelSpriteId.ShelfAmbient, 94, 108, frame);
        DrawSprite(canvas, PixelSpriteId.ShelfChilled, 198, 108, frame);
        DrawSprite(canvas, PixelSpriteId.ShelfFrozen, 302, 108, frame);

        var products = store?.Products ?? [];
        DrawProducts(canvas, products);

        Fill(canvas, 322, 106, 84, 5, "#9A6747");
        Fill(canvas, 326, 111, 76, 31, "#6B4634");
        var cashier = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Cashier);
        if (cashier is not null)
        {
            var cashierX = CharacterMotion.PingPong(
                frame.AnimationFrame,
                0,
                350,
                362,
                2,
                frame.ReduceMotion);
            DrawSprite(canvas, PixelSpriteId.Cashier, cashierX, 142, frame);
        }

        var restocker = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Restocker);
        if (restocker is not null)
        {
            var restockerX = CharacterMotion.PingPong(
                frame.AnimationFrame,
                9,
                30,
                230,
                4,
                frame.ReduceMotion);
            DrawSprite(canvas, PixelSpriteId.Restocker, restockerX, 142, frame);
        }

        DrawSupportingEmployees(canvas, employees, frame);

        var queueLength = Math.Min(
            operations?.CheckoutQueueLength ?? 0,
            PixelArtBudget.MaximumVisibleCustomers);
        for (var index = 0; index < queueLength; index++)
        {
            var anchorX = 302 - index * 28;
            var customerX = CharacterMotion.PingPong(
                frame.AnimationFrame,
                index * 3,
                anchorX - 12,
                anchorX,
                2,
                frame.ReduceMotion);
            DrawSprite(canvas, PixelSpriteId.Customer, customerX, 148, frame);
            Fill(canvas, customerX - 4, 151, 8, 3, "#F1B844");
        }
    }

    public void Dispose()
    {
        _atlasImage.Dispose();
        _atlas.Dispose();
        _paint.Dispose();
    }

    private void DrawProducts(
        SKCanvas canvas,
        IReadOnlyList<HajimaoDesktopShop.Application.Game.ProductSnapshot> products)
    {
        var zoneSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in products)
        {
            if (!ProductFrameIndexes.TryGetValue(product.Id, out var frameIndex))
            {
                continue;
            }

            var zone = product.ShelfKind;
            var slot = zoneSlots.GetValueOrDefault(zone);
            zoneSlots[zone] = slot + 1;
            var shelfStart = zone.ToLowerInvariant() switch
            {
                "chilled" => 166,
                "frozen" => 270,
                _ => 62
            };
            var x = shelfStart + slot * 16;
            DrawAtlasFrame(canvas, _atlas.ProductFrames[frameIndex], x, 88, 16, 16);
            var color = product.Quantity == 0
                ? "#E15A5A"
                : product.Quantity * 4 < product.Capacity ? "#F1B844" : "#72C986";
            Fill(canvas, x + 2, 105, 12, 3, color);
        }
    }

    private void DrawSupportingEmployees(
        SKCanvas canvas,
        IReadOnlyList<HajimaoDesktopShop.Application.Business.Employees.EmployeeOperationsEmployeeSnapshot> employees,
        BusinessShopSceneFrame frame)
    {
        var supporting = employees
            .Where(employee => employee.Role is not EmployeeRole.Cashier and not EmployeeRole.Restocker)
            .Take(2)
            .ToArray();
        for (var index = 0; index < supporting.Length; index++)
        {
            var anchorX = CharacterMotion.PingPong(
                frame.AnimationFrame,
                index * 11,
                112 + index * 40,
                176 + index * 40,
                4,
                frame.ReduceMotion);
            DrawSprite(canvas, PixelSpriteId.Customer, anchorX, 142, frame);
            Fill(canvas, anchorX - 5, 145, 10, 3, "#65B8C8");
        }
    }

    private void DrawSprite(
        SKCanvas canvas,
        PixelSpriteId spriteId,
        int anchorX,
        int anchorY,
        BusinessShopSceneFrame scene)
    {
        var frames = _atlas.GetFrames(spriteId);
        var normalizedFrame = CharacterMotion.FrameIndex(
            scene.AnimationFrame,
            frames.Count,
            scene.ReduceMotion);
        var frame = frames[normalizedFrame];
        DrawAtlasFrame(
            canvas,
            frame,
            anchorX - frame.AnchorX,
            anchorY - frame.AnchorY,
            frame.Width,
            frame.Height);
    }

    private void DrawAtlasFrame(
        SKCanvas canvas,
        PixelSpriteFrame frame,
        int x,
        int y,
        int width,
        int height)
    {
        var source = new SKRect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height);
        var destination = new SKRect(x, y, x + width, y + height);
        canvas.DrawImage(_atlasImage, source, destination, PixelSampling, _paint);
    }

    private void Fill(SKCanvas canvas, int x, int y, int width, int height, string color)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }
}
