namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopWindowPlacementPolicy
{
    public static bool TryRestore(
        DesktopPoint saved,
        DesktopSize windowSize,
        IReadOnlyList<DesktopRect> workAreas,
        double minimumVisible,
        out DesktopPoint restored)
    {
        ArgumentNullException.ThrowIfNull(workAreas);
        if (!double.IsFinite(minimumVisible) || minimumVisible <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumVisible),
                minimumVisible,
                "Minimum visibility must be positive and finite.");
        }

        var candidate = new DesktopRect(saved.X, saved.Y, windowSize.Width, windowSize.Height);
        foreach (var workArea in workAreas)
        {
            var intersectionWidth = Math.Max(
                0d,
                Math.Min(candidate.Right, workArea.Right) - Math.Max(candidate.X, workArea.X));
            var intersectionHeight = Math.Max(
                0d,
                Math.Min(candidate.Bottom, workArea.Bottom) - Math.Max(candidate.Y, workArea.Y));
            if (intersectionWidth >= minimumVisible && intersectionHeight >= minimumVisible)
            {
                restored = saved;
                return true;
            }
        }

        restored = default;
        return false;
    }

    public static bool TrySnapToNearestCorner(
        DesktopRect currentWindow,
        IReadOnlyList<DesktopRect> workAreas,
        double margin,
        out DesktopPoint snapped)
    {
        ArgumentNullException.ThrowIfNull(workAreas);
        if (!double.IsFinite(margin) || margin < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(margin),
                margin,
                "Margin must be non-negative and finite.");
        }

        if (workAreas.Count == 0)
        {
            snapped = default;
            return false;
        }

        var center = currentWindow.Center;
        var nearest = workAreas[0];
        var nearestDistance = SquaredDistance(center, nearest);
        for (var index = 1; index < workAreas.Count; index++)
        {
            var candidate = workAreas[index];
            var distance = SquaredDistance(center, candidate);
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        var snapLeft = center.X < nearest.Center.X;
        var snapTop = center.Y < nearest.Center.Y;
        snapped = new DesktopPoint(
            SnapAxis(nearest.X, nearest.Width, currentWindow.Width, margin, snapLeft),
            SnapAxis(nearest.Y, nearest.Height, currentWindow.Height, margin, snapTop));
        return true;
    }

    public static bool TrySnapAboveWorkAreaBottom(
        DesktopRect currentWindow,
        DesktopRect workArea,
        double threshold,
        double margin,
        out DesktopPoint snapped)
    {
        if (!double.IsFinite(threshold) || threshold < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "Threshold must be non-negative and finite.");
        }

        if (!double.IsFinite(margin) || margin < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(margin),
                margin,
                "Margin must be non-negative and finite.");
        }

        if (Math.Abs(currentWindow.Bottom - workArea.Bottom) > threshold)
        {
            snapped = default;
            return false;
        }

        snapped = new DesktopPoint(
            currentWindow.X,
            workArea.Bottom - currentWindow.Height - margin);
        return true;
    }

    public static DesktopPoint ConstrainToWorkArea(
        DesktopRect window,
        DesktopRect workArea,
        double margin)
    {
        if (!double.IsFinite(margin) || margin < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(margin),
                margin,
                "Margin must be non-negative and finite.");
        }

        return new DesktopPoint(
            ConstrainAxis(window.X, window.Width, workArea.X, workArea.Width, margin),
            ConstrainAxis(window.Y, window.Height, workArea.Y, workArea.Height, margin));
    }

    private static double SquaredDistance(DesktopPoint point, DesktopRect rectangle)
    {
        var horizontal = point.X < rectangle.X
            ? rectangle.X - point.X
            : point.X > rectangle.Right
                ? point.X - rectangle.Right
                : 0d;
        var vertical = point.Y < rectangle.Y
            ? rectangle.Y - point.Y
            : point.Y > rectangle.Bottom
                ? point.Y - rectangle.Bottom
                : 0d;
        return (horizontal * horizontal) + (vertical * vertical);
    }

    private static double SnapAxis(
        double workAreaStart,
        double workAreaLength,
        double windowLength,
        double margin,
        bool snapToStart)
    {
        if (windowLength + (2d * margin) > workAreaLength)
        {
            return workAreaStart + ((workAreaLength - windowLength) / 2d);
        }

        return snapToStart
            ? workAreaStart + margin
            : workAreaStart + workAreaLength - windowLength - margin;
    }

    private static double ConstrainAxis(
        double windowStart,
        double windowLength,
        double workAreaStart,
        double workAreaLength,
        double margin)
    {
        if (windowLength + (2d * margin) > workAreaLength)
        {
            return workAreaStart + ((workAreaLength - windowLength) / 2d);
        }

        return Math.Clamp(
            windowStart,
            workAreaStart + margin,
            workAreaStart + workAreaLength - windowLength - margin);
    }
}
