using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed class StoreDevelopment
{
    public const int MaximumExpansionLevel = 8;
    public const int MaximumShelfLevel = 9;
    public const int MaximumDecorationLevel = 9;

    private StoreDevelopment(StoreDevelopmentState state)
    {
        Validate(state);
        ExpansionLevel = state.ExpansionLevel;
        ShelfLevel = state.ShelfLevel;
        DecorationLevel = state.DecorationLevel;
    }

    public int ExpansionLevel { get; private set; }

    public int ShelfLevel { get; private set; }

    public int DecorationLevel { get; private set; }

    public int FloorAreaUnits => 1 + ExpansionLevel;

    public int ShelfSlotCount => 3 + (2 * ExpansionLevel);

    public int QueueComfortCapacity => 2 * ExpansionLevel;

    public int InventoryCapacityPermille => 1_000 + (250 * ShelfLevel);

    public int AttractionBonusBasisPoints =>
        (150 * ExpansionLevel) + (250 * DecorationLevel);

    public static StoreDevelopment CreateNew() =>
        new(new StoreDevelopmentState(0, 0, 0));

    public static StoreDevelopment Restore(StoreDevelopmentState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new StoreDevelopment(state);
    }

    public StoreDevelopmentState CaptureState() =>
        new(ExpansionLevel, ShelfLevel, DecorationLevel);

    public StoreUpgradeResult PreviewUpgrade(StoreUpgradeKind kind)
    {
        var currentLevel = GetLevel(kind);
        var maximumLevel = GetMaximumLevel(kind);
        var cost = GetUpgradeCost(kind, currentLevel + 1);

        if (currentLevel >= maximumLevel)
        {
            return new StoreUpgradeResult(StoreUpgradeStatus.MaximumLevel, Money.Zero);
        }

        if (kind is StoreUpgradeKind.Shelf or StoreUpgradeKind.Decoration &&
            currentLevel + 1 > ExpansionLevel + 1)
        {
            return new StoreUpgradeResult(StoreUpgradeStatus.PrerequisiteNotMet, cost);
        }

        return new StoreUpgradeResult(StoreUpgradeStatus.Success, cost);
    }

    internal void ApplyUpgrade(StoreUpgradeKind kind)
    {
        var preview = PreviewUpgrade(kind);
        if (preview.Status != StoreUpgradeStatus.Success)
        {
            throw new InvalidOperationException(
                $"Store upgrade '{kind}' cannot be applied with status '{preview.Status}'.");
        }

        switch (kind)
        {
            case StoreUpgradeKind.Expansion:
                ExpansionLevel++;
                break;
            case StoreUpgradeKind.Shelf:
                ShelfLevel++;
                break;
            case StoreUpgradeKind.Decoration:
                DecorationLevel++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private int GetLevel(StoreUpgradeKind kind) => kind switch
    {
        StoreUpgradeKind.Expansion => ExpansionLevel,
        StoreUpgradeKind.Shelf => ShelfLevel,
        StoreUpgradeKind.Decoration => DecorationLevel,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static int GetMaximumLevel(StoreUpgradeKind kind) => kind switch
    {
        StoreUpgradeKind.Expansion => MaximumExpansionLevel,
        StoreUpgradeKind.Shelf => MaximumShelfLevel,
        StoreUpgradeKind.Decoration => MaximumDecorationLevel,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static Money GetUpgradeCost(StoreUpgradeKind kind, int nextLevel)
    {
        var baseCost = kind switch
        {
            StoreUpgradeKind.Expansion => 60_000L,
            StoreUpgradeKind.Shelf => 25_000L,
            StoreUpgradeKind.Decoration => 30_000L,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        return new Money(checked(baseCost * nextLevel * nextLevel));
    }

    private static void Validate(StoreDevelopmentState state)
    {
        if (state.ExpansionLevel is < 0 or > MaximumExpansionLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (state.ShelfLevel is < 0 or > MaximumShelfLevel ||
            state.DecorationLevel is < 0 or > MaximumDecorationLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (state.ShelfLevel > state.ExpansionLevel + 1 ||
            state.DecorationLevel > state.ExpansionLevel + 1)
        {
            throw new ArgumentException(
                "Shelf and decoration levels cannot exceed expansion level plus one.",
                nameof(state));
        }
    }
}
