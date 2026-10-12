using SkiaSharp;

namespace HajimaoDesktopShop.Rendering.Characters;

public enum HumanCharacterAccessory
{
    None,
    Cap,
    Backpack,
    Briefcase,
    Camera,
    Cane,
    Helmet,
    Tote,
    Scarf,
    Hood,
    CollectorHat
}

public sealed record HumanCharacterVisualProfile(
    float ScaleX,
    float ScaleY,
    int GroundOffsetY,
    SKColor AccentColor,
    HumanCharacterAccessory Accessory);

public static class HumanCharacterVisualCatalog
{
    private static readonly string[] StreetArchetypes =
    [
        "early-commuter",
        "student",
        "office-worker",
        "tourist",
        "senior-neighbor",
        "delivery-rider",
        "family-shopper",
        "foodie",
        "bargain-hunter",
        "night-owl",
        "late-shift-worker",
        "collector"
    ];

    public static HumanCharacterVisualProfile Resolve(string archetypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        return archetypeId switch
        {
            "early-commuter" => Profile(0.98f, 1.00f, "#547AA5", HumanCharacterAccessory.Cap),
            "student" => Profile(0.92f, 0.94f, "#E6A65D", HumanCharacterAccessory.Backpack),
            "office-worker" => Profile(1.00f, 1.04f, "#514C69", HumanCharacterAccessory.Briefcase),
            "tourist" => Profile(1.04f, 1.00f, "#D96363", HumanCharacterAccessory.Camera),
            "senior-neighbor" => Profile(0.96f, 0.96f, "#7A5D45", HumanCharacterAccessory.Cane),
            "delivery-rider" => Profile(1.02f, 1.02f, "#3E7F75", HumanCharacterAccessory.Helmet),
            "family-shopper" => Profile(1.08f, 1.02f, "#6D8B57", HumanCharacterAccessory.Tote),
            "foodie" => Profile(1.06f, 1.00f, "#8A4F63", HumanCharacterAccessory.Scarf),
            "bargain-hunter" => Profile(0.96f, 1.00f, "#D3A43B", HumanCharacterAccessory.Tote),
            "night-owl" => Profile(0.94f, 0.98f, "#6A568E", HumanCharacterAccessory.Hood),
            "late-shift-worker" => Profile(1.02f, 1.04f, "#425C73", HumanCharacterAccessory.Cap),
            "collector" => Profile(1.10f, 1.08f, "#E1B84B", HumanCharacterAccessory.CollectorHat),
            _ => Profile(1.00f, 1.00f, "#6B7687", HumanCharacterAccessory.None)
        };
    }

    public static string ResolveStreetArchetype(int pedestrianIndex)
    {
        if (pedestrianIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pedestrianIndex));
        }

        return StreetArchetypes[pedestrianIndex % StreetArchetypes.Length];
    }

    private static HumanCharacterVisualProfile Profile(
        float scaleX,
        float scaleY,
        string accentColor,
        HumanCharacterAccessory accessory) =>
        new(scaleX, scaleY, GroundOffsetY: 0, SKColor.Parse(accentColor), accessory);
}
