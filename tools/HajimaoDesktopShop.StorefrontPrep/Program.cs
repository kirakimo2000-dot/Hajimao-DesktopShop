using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: StorefrontPrep <source-atlas.png> <output-directory>");
    return 2;
}

StorefrontTexturePreparer.Prepare(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
return 0;

internal static class StorefrontTexturePreparer
{
    private sealed record Cell(string Name, Rectangle Bounds);

    public static void Prepare(string sourcePath, string outputDirectory)
    {
        using var source = new Bitmap(sourcePath);
        using var transparent = RemoveConnectedNeutralBackground(source);
        Directory.CreateDirectory(outputDirectory);

        var x1 = (int)Math.Round(transparent.Width * 0.365d);
        var x2 = (int)Math.Round(transparent.Width * 0.651d);
        var y1 = transparent.Height / 3;
        var y2 = transparent.Height * 2 / 3;
        var cells = new[]
        {
            new Cell("convenience.png", new Rectangle(0, 0, x1, y1)),
            new Cell("commuter.png", new Rectangle(x1, 0, x2 - x1, y1)),
            new Cell("discount.png", new Rectangle(x2, 0, transparent.Width - x2, y1)),
            new Cell("warehouse.png", new Rectangle(0, y1, x1, y2 - y1)),
            new Cell("lifestyle.png", new Rectangle(x1, y1, x2 - x1, y2 - y1)),
            new Cell("health.png", new Rectangle(x2, y1, transparent.Width - x2, y2 - y1)),
            new Cell("premium.png", new Rectangle(0, y2, x1, transparent.Height - y2)),
            new Cell("variety.png", new Rectangle(x1, y2, x2 - x1, transparent.Height - y2))
        };

        foreach (var cell in cells)
        {
            var subject = FindOpaqueBounds(transparent, cell.Bounds);
            if (subject.IsEmpty)
            {
                throw new InvalidDataException($"No storefront pixels found for {cell.Name}.");
            }

            subject.Inflate(2, 2);
            subject.Intersect(cell.Bounds);
            using var output = new Bitmap(224, 102, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(output);
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.SmoothingMode = SmoothingMode.None;

            var scale = Math.Min(224d / subject.Width, 100d / subject.Height);
            var width = Math.Max(1, (int)Math.Round(subject.Width * scale));
            var height = Math.Max(1, (int)Math.Round(subject.Height * scale));
            var destination = new Rectangle((224 - width) / 2, 102 - height, width, height);
            graphics.DrawImage(
                transparent,
                destination,
                subject.X,
                subject.Y,
                subject.Width,
                subject.Height,
                GraphicsUnit.Pixel);
            output.Save(Path.Combine(outputDirectory, cell.Name), ImageFormat.Png);
        }
    }

    private static Bitmap RemoveConnectedNeutralBackground(Bitmap source)
    {
        var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            var visited = new bool[bitmap.Width * bitmap.Height];
            var queue = new Queue<int>();

            void Enqueue(int x, int y)
            {
                if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height)
                {
                    return;
                }

                var index = y * bitmap.Width + x;
                if (visited[index] || !IsNeutralBackground(bytes, stride, x, y))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < bitmap.Width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, bitmap.Height - 1);
            }

            for (var y = 0; y < bitmap.Height; y++)
            {
                Enqueue(0, y);
                Enqueue(bitmap.Width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % bitmap.Width;
                var y = index / bitmap.Width;
                bytes[y * stride + x * 4 + 3] = 0;
                Enqueue(x - 1, y);
                Enqueue(x + 1, y);
                Enqueue(x, y - 1);
                Enqueue(x, y + 1);
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    private static bool IsNeutralBackground(byte[] bytes, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var blue = bytes[offset];
        var green = bytes[offset + 1];
        var red = bytes[offset + 2];
        var minimum = Math.Min(red, Math.Min(green, blue));
        var maximum = Math.Max(red, Math.Max(green, blue));
        return minimum >= 232 && maximum - minimum <= 10;
    }

    private static Rectangle FindOpaqueBounds(Bitmap bitmap, Rectangle search)
    {
        var left = search.Right;
        var top = search.Bottom;
        var right = search.Left - 1;
        var bottom = search.Top - 1;
        for (var y = search.Top; y < search.Bottom; y++)
        {
            for (var x = search.Left; x < search.Right; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
