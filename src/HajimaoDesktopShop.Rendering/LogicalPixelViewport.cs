namespace HajimaoDesktopShop.Rendering;

public static class LogicalPixelViewport
{
    public static bool TryMapPoint(
        int viewportWidth,
        int viewportHeight,
        int logicalWidth,
        int logicalHeight,
        double viewportX,
        double viewportY,
        out LogicalPixelPoint point)
    {
        point = default;
        if (viewportWidth <= 0
            || viewportHeight <= 0
            || logicalWidth <= 0
            || logicalHeight <= 0
            || !double.IsFinite(viewportX)
            || !double.IsFinite(viewportY))
        {
            return false;
        }

        var scale = Math.Max(
            1,
            Math.Min(viewportWidth / logicalWidth, viewportHeight / logicalHeight));
        var offsetX = (viewportWidth - logicalWidth * scale) / 2;
        var offsetY = (viewportHeight - logicalHeight * scale) / 2;
        var logicalX = (int)Math.Floor((viewportX - offsetX) / scale);
        var logicalY = (int)Math.Floor((viewportY - offsetY) / scale);
        if (logicalX < 0
            || logicalX >= logicalWidth
            || logicalY < 0
            || logicalY >= logicalHeight)
        {
            return false;
        }

        point = new LogicalPixelPoint(logicalX, logicalY);
        return true;
    }
}
