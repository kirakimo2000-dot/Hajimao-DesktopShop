using HajimaoDesktopShop.Domain.Employees;
using SkiaSharp;

namespace HajimaoDesktopShop.Rendering;

public sealed class BusinessShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;

    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

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
        DrawShelf(canvas, 54, 30, 92, "#D7A64C");
        DrawShelf(canvas, 158, 30, 92, "#65B8C8");
        DrawShelf(canvas, 262, 30, 78, "#6E91CF");

        var products = store?.Products ?? [];
        for (var index = 0; index < Math.Min(products.Count, 12); index++)
        {
            var product = products[index];
            var color = product.Quantity == 0
                ? "#E15A5A"
                : product.Quantity * 4 < product.Capacity ? "#F1B844" : "#72C986";
            Fill(canvas, 66 + index % 4 * 16 + index / 4 * 104, 96, 10, 5, color);
        }

        Fill(canvas, 322, 106, 84, 5, "#9A6747");
        Fill(canvas, 326, 111, 76, 31, "#6B4634");
        var cashier = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Cashier);
        if (cashier is not null)
        {
            DrawPerson(canvas, 350, 72, "#F1B844");
        }

        var restocker = employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Restocker);
        if (restocker is not null)
        {
            DrawPerson(canvas, 22, 90, "#72C986");
        }

        var queueLength = Math.Min(operations?.CheckoutQueueLength ?? 0, 5);
        for (var index = 0; index < queueLength; index++)
        {
            var x = 286 - index * 24;
            DrawPerson(canvas, x, 108, "#F1B844");
            Fill(canvas, x + 4, 112, 8, 4, "#F1B844");
        }
    }

    public void Dispose() => _paint.Dispose();

    private void DrawShelf(SKCanvas canvas, int x, int y, int width, string goodsColor)
    {
        Fill(canvas, x, y, width, 78, "#241D1B");
        Fill(canvas, x + 4, y + 4, width - 8, 70, "#5B4638");
        Fill(canvas, x + 10, y + 32, width - 20, 32, goodsColor);
        Fill(canvas, x + 8, y + 24, width - 16, 4, "#241D1B");
    }

    private void DrawPerson(SKCanvas canvas, int x, int y, string bodyColor)
    {
        Fill(canvas, x + 4, y, 14, 12, "#E7B993");
        Fill(canvas, x + 2, y + 12, 18, 22, bodyColor);
        Fill(canvas, x + 7, y + 5, 3, 3, "#23262C");
        Fill(canvas, x, y + 30, 8, 4, "#2A2D35");
        Fill(canvas, x + 14, y + 30, 8, 4, "#2A2D35");
    }

    private void Fill(SKCanvas canvas, int x, int y, int width, int height, string color)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }
}
