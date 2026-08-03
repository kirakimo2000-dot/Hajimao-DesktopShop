using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Simulation.Employees;
using SkiaSharp;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Rendering;

public sealed class ShopSceneRenderer : IDisposable
{
    public const int LogicalWidth = 420;
    public const int LogicalHeight = 180;

    private readonly SKPaint _paint = new()
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    public void Render(SKCanvas canvas, SKImageInfo target, SimulationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        canvas.Clear(SKColor.Parse("#17191D"));
        var scale = Math.Max(1, Math.Min(target.Width / LogicalWidth, target.Height / LogicalHeight));
        var sceneWidth = LogicalWidth * scale;
        var sceneHeight = LogicalHeight * scale;
        var offsetX = (target.Width - sceneWidth) / 2;
        var offsetY = (target.Height - sceneHeight) / 2;

        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale);
        canvas.ClipRect(new SKRect(0, 0, LogicalWidth, LogicalHeight));
        DrawLogicalScene(canvas, snapshot);
        canvas.Restore();
    }

    public void DrawLogicalScene(SKCanvas canvas, SimulationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(snapshot);
        DrawRoom(canvas);
        DrawShelves(canvas, snapshot);
        DrawCounter(canvas);
        DrawEmployees(canvas, snapshot);
        DrawCustomers(canvas, snapshot);
    }

    public void Dispose() => _paint.Dispose();

    private void DrawRoom(SKCanvas canvas)
    {
        Fill(canvas, 0, 0, LogicalWidth, 124, "#4A353C");
        Fill(canvas, 0, 124, LogicalWidth, 56, "#B87349");
        Fill(canvas, 0, 152, LogicalWidth, 4, "#8F5338");
        Fill(canvas, 0, 176, LogicalWidth, 4, "#8F5338");
        Fill(canvas, 0, 120, LogicalWidth, 4, "#31262A");
    }

    private void DrawShelves(SKCanvas canvas, SimulationSnapshot snapshot)
    {
        DrawShelf(canvas, 58, 28, 92, 78, "#241D1B", "#5B4638", "#D7A64C");
        DrawShelf(canvas, 162, 28, 92, 78, "#1C2C32", "#38515B", "#65B8C8");
        DrawShelf(canvas, 266, 28, 78, 78, "#20283D", "#3B4664", "#6E91CF");

        DrawStockIndicators(canvas, snapshot, "ambient", 70, 94);
        DrawStockIndicators(canvas, snapshot, "chilled", 174, 94);
        DrawStockIndicators(canvas, snapshot, "frozen", 278, 94);
    }

    private void DrawShelf(
        SKCanvas canvas,
        int x,
        int y,
        int width,
        int height,
        string border,
        string frame,
        string goods)
    {
        Fill(canvas, x, y, width, height, border);
        Fill(canvas, x + 3, y + 3, width - 6, height - 6, frame);
        Fill(canvas, x + 11, y + 32, width - 22, 34, goods);
        Fill(canvas, x + 8, y + 24, width - 16, 4, border);
        Fill(canvas, x + 8, y + height - 10, width - 16, 4, border);
    }

    private void DrawStockIndicators(
        SKCanvas canvas,
        SimulationSnapshot snapshot,
        string shelfKind,
        int startX,
        int y)
    {
        var products = snapshot.Shop.Products
            .Where(product => string.Equals(product.ShelfKind, shelfKind, StringComparison.Ordinal))
            .Take(5)
            .ToArray();
        for (var index = 0; index < products.Length; index++)
        {
            var product = products[index];
            var color = product.Quantity == 0
                ? "#E15A5A"
                : product.Quantity * 4 < product.Capacity ? "#F1B844" : "#72C986";
            Fill(canvas, startX + (index * 12), y, 8, 5, color);
        }
    }

    private void DrawCounter(SKCanvas canvas)
    {
        Fill(canvas, 324, 108, 78, 34, "#2A1D18");
        Fill(canvas, 328, 112, 70, 26, "#6B4634");
        Fill(canvas, 320, 104, 86, 5, "#9A6747");
    }

    private void DrawEmployees(SKCanvas canvas, SimulationSnapshot snapshot)
    {
        var cashier = snapshot.Employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Cashier);
        var restocker = snapshot.Employees.FirstOrDefault(employee => employee.Role == EmployeeRole.Restocker);
        DrawPerson(canvas, 350, 72, "#F1B844", "#F1C7A5", cashier?.State == EmployeeState.Working);
        DrawPerson(canvas, 20, 88, "#65C985", "#D5A985", restocker?.State == EmployeeState.Working);
    }

    private void DrawCustomers(SKCanvas canvas, SimulationSnapshot snapshot)
    {
        foreach (var customer in snapshot.Customers)
        {
            var (x, y) = customer.State switch
            {
                CustomerState.Entering => (16, 118),
                CustomerState.SeekingProduct => (112 + ((int)(customer.Id % 3) * 44), 68),
                CustomerState.Queueing => (286 + ((int)(customer.Id % 2) * 20), 114),
                CustomerState.CheckingOut => (340, 88),
                CustomerState.Leaving => (388, 118),
                _ => (16, 118)
            };
            DrawPerson(canvas, x, y, "#72C986", "#E7B993", isWorking: false);
            var markerColor = customer.State switch
            {
                CustomerState.Queueing => "#F1B844",
                CustomerState.CheckingOut => "#65B8C8",
                CustomerState.Leaving => "#E15A5A",
                _ => "#F4EBDD"
            };
            Fill(canvas, x + 6, y - 5, 6, 3, markerColor);
        }
    }

    private void DrawPerson(
        SKCanvas canvas,
        int x,
        int y,
        string bodyColor,
        string skinColor,
        bool isWorking)
    {
        Fill(canvas, x + 4, y, 14, 12, skinColor);
        Fill(canvas, x + 2, y + 12, 18, 22, bodyColor);
        Fill(canvas, x + 7, y + 5, 3, 3, "#23262C");
        Fill(canvas, x, y + 30, 8, 4, "#2A2D35");
        Fill(canvas, x + 14, y + 30, 8, 4, "#2A2D35");
        if (isWorking)
        {
            Fill(canvas, x + 18, y + 14, 6, 6, "#F4EBDD");
        }
    }

    private void Fill(SKCanvas canvas, int x, int y, int width, int height, string color)
    {
        _paint.Color = SKColor.Parse(color);
        canvas.DrawRect(x, y, width, height, _paint);
    }
}
