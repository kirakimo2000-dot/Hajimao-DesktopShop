namespace HajimaoDesktopShop.Rendering.PixelArt;

public static class CharacterAnimation
{
    public static int CelIndex(long presentationFrame, bool reduceMotion)
    {
        if (reduceMotion)
        {
            return 0;
        }

        var logicalFrame = (int)(
            (presentationFrame % PixelArtBudget.CharacterAnimationFrameCount
                + PixelArtBudget.CharacterAnimationFrameCount)
            % PixelArtBudget.CharacterAnimationFrameCount);
        return logicalFrame % PixelArtBudget.StoredCharacterCelCount;
    }
}
