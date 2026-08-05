namespace HajimaoDesktopShop.Desktop.Services;

public readonly record struct DesktopPoint
{
    public DesktopPoint(double x, double y)
    {
        EnsureFinite(x, nameof(x));
        EnsureFinite(y, nameof(y));
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Coordinate must be finite.");
        }
    }
}

public readonly record struct DesktopSize
{
    public DesktopSize(double width, double height)
    {
        EnsurePositiveFinite(width, nameof(width));
        EnsurePositiveFinite(height, nameof(height));
        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }

    private static void EnsurePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Dimension must be positive and finite.");
        }
    }
}

public readonly record struct DesktopRect
{
    public DesktopRect(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Coordinate must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Coordinate must be finite.");
        }

        if (!double.IsFinite(width) || width <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Dimension must be positive and finite.");
        }

        if (!double.IsFinite(height) || height <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Dimension must be positive and finite.");
        }

        if (!double.IsFinite(x + width))
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Rectangle extent must be finite.");
        }

        if (!double.IsFinite(y + height))
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Rectangle extent must be finite.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public DesktopPoint Center => new(X + (Width / 2d), Y + (Height / 2d));
}
