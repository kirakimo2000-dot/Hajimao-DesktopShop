using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.PixelArt;

public static class CharacterSpriteAudit
{
    public static CharacterSpriteAuditResult Analyze(SKBitmap bitmap, PixelSpriteFrame frame)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentOutOfRangeException.ThrowIfNegative(frame.X);
        ArgumentOutOfRangeException.ThrowIfNegative(frame.Y);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.Width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frame.Height, 1);
        if (frame.X + frame.Width > bitmap.Width || frame.Y + frame.Height > bitmap.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Sprite frame falls outside the bitmap.");
        }

        var visited = new bool[frame.Width * frame.Height];
        var componentSizes = new List<int>();
        var visiblePixelCount = 0;
        var minX = frame.Width;
        var minY = frame.Height;
        var maxX = -1;
        var maxY = -1;

        for (var localY = 0; localY < frame.Height; localY++)
        {
            for (var localX = 0; localX < frame.Width; localX++)
            {
                if (!IsVisible(bitmap, frame, localX, localY))
                {
                    continue;
                }

                visiblePixelCount++;
                minX = Math.Min(minX, localX);
                minY = Math.Min(minY, localY);
                maxX = Math.Max(maxX, localX);
                maxY = Math.Max(maxY, localY);
                var index = localY * frame.Width + localX;
                if (!visited[index])
                {
                    componentSizes.Add(MeasureComponent(bitmap, frame, localX, localY, visited));
                }
            }
        }

        componentSizes.Sort(static (left, right) => right.CompareTo(left));
        return visiblePixelCount == 0
            ? new CharacterSpriteAuditResult(
                0,
                Array.Empty<int>(),
                frame.Width,
                frame.Height,
                frame.Width,
                frame.Height)
            : new CharacterSpriteAuditResult(
                visiblePixelCount,
                Array.AsReadOnly(componentSizes.ToArray()),
                minX,
                minY,
                frame.Width - maxX - 1,
                frame.Height - maxY - 1);
    }

    private static int MeasureComponent(
        SKBitmap bitmap,
        PixelSpriteFrame frame,
        int startX,
        int startY,
        bool[] visited)
    {
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        visited[startY * frame.Width + startX] = true;
        var size = 0;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            size++;
            Visit(x - 1, y);
            Visit(x + 1, y);
            Visit(x, y - 1);
            Visit(x, y + 1);
        }

        return size;

        void Visit(int x, int y)
        {
            if (x < 0 || x >= frame.Width || y < 0 || y >= frame.Height)
            {
                return;
            }

            var index = y * frame.Width + x;
            if (visited[index] || !IsVisible(bitmap, frame, x, y))
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue((x, y));
        }
    }

    private static bool IsVisible(SKBitmap bitmap, PixelSpriteFrame frame, int x, int y) =>
        bitmap.GetPixel(frame.X + x, frame.Y + y).Alpha > 0;
}

public sealed record CharacterSpriteAuditResult(
    int VisiblePixelCount,
    IReadOnlyList<int> ComponentSizes,
    int LeftPadding,
    int TopPadding,
    int RightPadding,
    int BottomPadding)
{
    public bool IsValid =>
        VisiblePixelCount > 0
        && ComponentSizes.Count == 1
        && LeftPadding > 0
        && TopPadding > 0
        && RightPadding > 0
        && BottomPadding > 0;
}
