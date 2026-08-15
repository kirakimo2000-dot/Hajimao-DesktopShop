namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed record StoreCombatProfile(
    int ArrivalModifierPermille,
    int ActiveCustomerCapacityPermille,
    int RewardModifierPermille,
    int DemandHpModifierPermille,
    int MovementModifierPermille,
    string ProfitStyleText,
    string RiskText);

public static class StoreCombatProfilePolicy
{
    private static readonly StoreCombatProfile Convenience = new(
        1_000,
        1_000,
        1_000,
        1_000,
        1_000,
        "收益与客流均衡",
        "均衡风险");

    public static StoreCombatProfile Resolve(string storeFormatId) => storeFormatId switch
    {
        "discount" => new StoreCombatProfile(
            1_300,
            1_250,
            850,
            900,
            1_150,
            "薄利高客流",
            "顾客移动更快，漏客风险较高"),
        "premium" => new StoreCombatProfile(
            750,
            750,
            1_500,
            1_300,
            900,
            "低客流高单次收益",
            "顾客需求更高，组合不足时风险较高"),
        _ => Convenience
    };
}
