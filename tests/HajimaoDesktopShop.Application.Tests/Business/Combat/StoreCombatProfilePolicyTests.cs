using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class StoreCombatProfilePolicyTests
{
    [Fact]
    public void Formats_CreateDistinctProfitTrafficAndRiskProfiles()
    {
        var convenience = StoreCombatProfilePolicy.Resolve("convenience");
        var discount = StoreCombatProfilePolicy.Resolve("discount");
        var premium = StoreCombatProfilePolicy.Resolve("premium");

        Assert.True(discount.ArrivalModifierPermille > convenience.ArrivalModifierPermille);
        Assert.True(discount.RewardModifierPermille < convenience.RewardModifierPermille);
        Assert.True(discount.MovementModifierPermille > convenience.MovementModifierPermille);
        Assert.True(premium.ArrivalModifierPermille < convenience.ArrivalModifierPermille);
        Assert.True(premium.RewardModifierPermille > convenience.RewardModifierPermille);
        Assert.True(premium.DemandHpModifierPermille > convenience.DemandHpModifierPermille);
        Assert.Equal(3, new[] { convenience.RiskText, discount.RiskText, premium.RiskText }.Distinct().Count());
    }

    [Fact]
    public void UnknownFormat_UsesNeutralProfile()
    {
        var profile = StoreCombatProfilePolicy.Resolve("legacy");

        Assert.Equal(1_000, profile.ArrivalModifierPermille);
        Assert.Equal(1_000, profile.RewardModifierPermille);
        Assert.Equal("均衡风险", profile.RiskText);
    }
}
