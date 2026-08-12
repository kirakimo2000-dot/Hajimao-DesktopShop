using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Rendering.Customers;
using HajimaoDesktopShop.Rendering.Interactions;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class BusinessShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;

    private static readonly SKSamplingOptions PixelSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    private static readonly string[] VariantColors =
        ["#65B8C8", "#F1B844", "#E15A5A", "#72C986", "#B58AD4", "#E8905B", "#A6ABB4", "#F4EBDD"];

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
        foreach (var pose in BusinessShopEmployeeChoreography.CreatePoses(
                     employees,
                     frame.AnimationFrame,
                     frame.ReduceMotion))
        {
            var sprite = pose.Role switch
            {
                EmployeeRole.Cashier => PixelSpriteId.Cashier,
                EmployeeRole.Restocker => PixelSpriteId.Restocker,
                _ => PixelSpriteId.Customer
            };
            DrawSprite(canvas, sprite, pose.X, pose.Y, frame, pose.AppearanceKey);
            if (pose.IsSupporting)
            {
                Fill(canvas, pose.X - 5, pose.Y + 3, 10, 3, "#65B8C8");
            }
        }

        var journeyDrawn = DrawCustomerJourney(canvas, store, operations, frame);

        var queueLength = Math.Min(
            operations?.CheckoutQueueLength ?? 0,
            PixelArtBudget.MaximumVisibleCustomers - (journeyDrawn ? 1 : 0));
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

    private bool DrawCustomerJourney(
        SKCanvas canvas,
        BusinessStoreSnapshot? store,
        StoreOperationsSnapshot? operations,
        BusinessShopSceneFrame frame)
    {
        if (store is null
            || operations is null
            || operations.Visitors <= 0
            || store.Products.Count == 0)
        {
            return false;
        }

        var products = store.Products
            .OrderBy(product => product.Id, StringComparer.Ordinal)
            .ToArray();
        var productIndex = (int)(((long)operations.AcceptedPurchases
            + operations.Visitors
            - 1L) % products.Length);
        var product = products[productIndex];
        var pose = BusinessShopCustomerChoreography.CreatePose(
            product.ShelfKind,
            frame.AnimationFrame,
            operations.Visitors,
            frame.ReduceMotion);

        DrawSprite(canvas, PixelSpriteId.Customer, pose.X, pose.Y, frame);
        if (pose.CarryingProduct)
        {
            Fill(canvas, pose.X - 3, pose.Y - 18, 6, 4, "#F1B844");
        }

        return true;
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
            ProductSpriteVariant variant;
            try
            {
                variant = ContentSpriteKey.ResolveProduct(product.IconKey);
            }
            catch (ArgumentException)
            {
                variant = new ProductSpriteVariant(0, 0);
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
            DrawAtlasFrame(canvas, _atlas.ProductFrames[variant.FrameIndex], x, 88, 16, 16);
            Fill(canvas, x + 11, 90, 3, 3, VariantColors[variant.PaletteIndex]);
            var color = product.Quantity == 0
                ? "#E15A5A"
                : product.Quantity * 4 < product.Capacity ? "#F1B844" : "#72C986";
            Fill(canvas, x + 2, 105, 12, 3, color);
        }
    }

    private void DrawSprite(
        SKCanvas canvas,
        PixelSpriteId spriteId,
        int anchorX,
        int anchorY,
        BusinessShopSceneFrame scene,
        string? appearanceKey = null)
    {
        var frame = spriteId is PixelSpriteId.Cashier
            or PixelSpriteId.Restocker
            or PixelSpriteId.Customer
                ? _atlas.GetCharacterFrame(spriteId, scene.AnimationFrame, scene.ReduceMotion)
                : _atlas.GetFrames(spriteId)[0];
        DrawAtlasFrame(
            canvas,
            frame,
            anchorX - frame.AnchorX,
            anchorY - frame.AnchorY,
            frame.Width,
            frame.Height);
        if (appearanceKey is not null)
        {
            EmployeeSpriteVariant appearance;
            try
            {
                appearance = ContentSpriteKey.ResolveEmployee(appearanceKey);
            }
            catch (ArgumentException)
            {
                appearance = ContentSpriteKey.ResolveEmployee("employee-a01");
            }

            Fill(
                canvas,
                anchorX - 5 + appearance.DetailIndex % 2,
                anchorY - 29,
                10,
                3,
                VariantColors[appearance.PaletteIndex % VariantColors.Length]);
            Fill(
                canvas,
                anchorX - 4,
                anchorY - 16,
                8,
                2,
                VariantColors[appearance.DetailIndex]);
        }
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
