using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Application.Business.Procurement;

public sealed record ProcurementChannel(
    string Id,
    string Name,
    int CostPermille,
    int MinimumOrderQuantity,
    int DeliveryMinutes)
{
    public static IReadOnlyList<ProcurementChannel> DefaultChannels { get; } = Array.AsReadOnly(
    new ProcurementChannel[]
    {
        new("local-wholesale", "本地批发", 1_250, 1, 0),
        new("regional-distributor", "区域配送", 1_000, 6, 30),
        new("direct-manufacturer", "厂家直供", 850, 24, 120)
    });

    public Money QuoteUnitCost(Money wholesalePrice)
    {
        if (!wholesalePrice.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(wholesalePrice));
        }

        var scaled = checked(wholesalePrice.Cents * CostPermille);
        return new Money(checked((scaled + 999L) / 1_000L));
    }
}
