namespace HajimaoDesktopShop.Rendering.PixelArt;

public static class CharacterMotion
{
    public static int FrameIndex(long presentationTick, int frameCount, bool reduceMotion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frameCount, 1);
        if (reduceMotion)
        {
            return 0;
        }

        return (int)((presentationTick % frameCount + frameCount) % frameCount);
    }

    public static int HorizontalLoop(
        long presentationTick,
        int actorSeed,
        int start,
        int end,
        int step,
        bool reduceMotion)
    {
        ValidateTrack(start, end, step);
        var positions = checked(((end - start) / step) + 1);
        var seededStep = PositiveModulo(actorSeed, positions);
        if (reduceMotion)
        {
            return checked(start + seededStep * step);
        }

        var currentStep = PositiveModulo(presentationTick + seededStep, positions);
        return checked(start + currentStep * step);
    }

    public static int PingPong(
        long presentationTick,
        int actorSeed,
        int start,
        int end,
        int step,
        bool reduceMotion)
    {
        ValidateTrack(start, end, step);
        var forwardSteps = (end - start) / step;
        if (reduceMotion || forwardSteps == 0)
        {
            return HorizontalLoop(0, actorSeed, start, end, step, reduceMotion: true);
        }

        var period = checked(forwardSteps * 2);
        var phase = PositiveModulo(presentationTick + actorSeed, period);
        var distance = phase <= forwardSteps ? phase : period - phase;
        return checked(start + distance * step);
    }

    private static void ValidateTrack(int start, int end, int step)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(step, 1);
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "Track end must not precede start.");
        }

        if ((end - start) % step != 0)
        {
            throw new ArgumentException("Track length must be divisible by step.", nameof(step));
        }
    }

    private static int PositiveModulo(long value, int modulus) =>
        (int)((value % modulus + modulus) % modulus);
}
