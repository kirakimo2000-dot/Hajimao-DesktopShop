using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class InvestmentCandidateCardViewModel
{
    internal InvestmentCandidateCardViewModel(
        InvestmentCandidate candidate,
        Action<InvestmentCandidateCardViewModel> invest)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(invest);
        Candidate = candidate;
        InvestCommand = new RelayCommand(() => invest(this), () => candidate.IsExecutable);
    }

    internal InvestmentCandidate Candidate { get; }

    public string Id => Candidate.Id;

    public string TitleText => Candidate.Kind switch
    {
        InvestmentKind.Expansion => "扩建店面",
        InvestmentKind.Shelf => "升级货架",
        InvestmentKind.Decoration => "店铺装修",
        InvestmentKind.Employee => $"{Candidate.TargetName} · {RoleText(Candidate.Effect.AddedRole)}",
        InvestmentKind.OpenStore => $"开设 {Candidate.TargetName}",
        _ => Candidate.Kind.ToString()
    };

    public string CostText => $"投入 {FormatMoney(Candidate.Return.CostCents)}";

    public string ExpectedBenefitText => Candidate.Kind == InvestmentKind.OpenStore
        ? "新店尚无完整经营数据"
        : Candidate.Return.ExpectedDailyNetBenefitCents switch
        {
            > 0 => $"保守估计 +{FormatMoney(Candidate.Return.ExpectedDailyNetBenefitCents)}/经营日",
            < 0 => $"保守估计 -{FormatMoney(-Candidate.Return.ExpectedDailyNetBenefitCents)}/经营日",
            _ => "暂无足够数据"
        };

    public string PaybackText => Candidate.Kind == InvestmentKind.OpenStore
        ? "新店日结后评估回本"
        : Candidate.Return.PaybackDaysTenths is { } payback
        ? string.Format(CultureInfo.InvariantCulture, "预计 {0:0.0} 天回本", payback / 10m)
        : "等待经营证据";

    public string CashAfterText => $"投资后现金 {FormatMoney(Candidate.Return.CashAfterInvestmentCents)}";

    public string CashPressureText => Candidate.Return.CashPressure switch
    {
        InvestmentCashPressure.Healthy => "现金储备健康",
        InvestmentCashPressure.Tight => "现金偏紧：不足两个经营周期",
        InvestmentCashPressure.Critical => "高风险：不足一个经营周期",
        InvestmentCashPressure.CannotAfford => "无法支付",
        _ => "缺少完整支出基准"
    };

    public string EffectText
    {
        get
        {
            var effects = new List<string>();
            if (Candidate.Effect.ShelfSlotChange != 0)
            {
                effects.Add($"货架位 +{Candidate.Effect.ShelfSlotChange}");
            }

            if (Candidate.Effect.QueueComfortChange != 0)
            {
                effects.Add($"舒适排队 +{Candidate.Effect.QueueComfortChange}");
            }

            if (Candidate.Effect.InventoryCapacityChangePermille != 0)
            {
                effects.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "库存容量 +{0:0}%",
                    Candidate.Effect.InventoryCapacityChangePermille / 10m));
            }

            if (Candidate.Effect.AttractionChangeBasisPoints != 0)
            {
                effects.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "吸引力 +{0:0.0}%",
                    Candidate.Effect.AttractionChangeBasisPoints / 100m));
            }

            if (Candidate.Effect.AddedRole is { } role)
            {
                effects.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "新增{0} · 效率 {1:0}%",
                    RoleText(role),
                    Candidate.Effect.AddedEfficiencyPermille / 10m));
            }

            if (Candidate.Effect.StoreCountChange != 0)
            {
                effects.Add($"新增店铺 +{Candidate.Effect.StoreCountChange}");
            }

            return string.Join(" · ", effects);
        }
    }

    public string EstimateConditionText => Candidate.EstimateCondition switch
    {
        InvestmentEstimateCondition.QueueLossesRepeat => "前提：排队流失在后续经营日重复出现",
        InvestmentEstimateCondition.StockLossesRepeat => "前提：缺货流失在后续经营日重复出现",
        InvestmentEstimateCondition.TrafficConversionStaysStable => "前提：当前进店转化率保持稳定",
        InvestmentEstimateCondition.RoleBottleneckPersists => "前提：当前岗位瓶颈持续存在",
        InvestmentEstimateCondition.NewStoreNeedsCompletedDay =>
            "新店需完成一个经营日后再评估回报",
        _ => "当前数据不足，暂不估算收益"
    };

    public string AvailabilityText => Candidate.Availability switch
    {
        InvestmentAvailability.InsufficientFunds => "资金不足",
        InvestmentAvailability.PrerequisiteNotMet => "需先扩建",
        InvestmentAvailability.LevelLocked => $"需要 Lv.{Candidate.RequiredPlayerLevel}",
        _ => "可投资"
    };

    public IRelayCommand InvestCommand { get; }

    private static string RoleText(EmployeeRole? role) => role switch
    {
        EmployeeRole.Cashier => "收银员",
        EmployeeRole.Restocker => "补货员",
        EmployeeRole.SalesAssistant => "导购员",
        EmployeeRole.Cleaner => "清洁员",
        EmployeeRole.Manager => "店长",
        EmployeeRole.Buyer => "采购员",
        _ => "员工"
    };

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}
