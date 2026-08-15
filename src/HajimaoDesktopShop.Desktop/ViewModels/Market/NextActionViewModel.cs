using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class NextActionViewModel : ObservableObject
{
    private string _contextText = "挂机进行中";
    private string _title = "等待毛毛完成首位顾客";
    private string _detailText = "无需操作；保持游戏运行，毛毛会自动投掷商品。";
    private string _actionText = "查看战斗";
    private ManagementSection _suggestedSection = ManagementSection.Overview;

    public string ContextText { get => _contextText; private set => SetProperty(ref _contextText, value); }
    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public string DetailText { get => _detailText; private set => SetProperty(ref _detailText, value); }
    public string ActionText { get => _actionText; private set => SetProperty(ref _actionText, value); }
    public ManagementSection SuggestedSection { get => _suggestedSection; private set => SetProperty(ref _suggestedSection, value); }

    public void Update(
        BusinessCombatSnapshot snapshot,
        string selectedStoreId,
        bool canOpenNewStore)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedStoreId);
        var store = snapshot.Stores.SingleOrDefault(item => item.StoreId == selectedStoreId);
        if (store is null)
        {
            Set("未开放店铺", "先从街区选择已开店铺", "未开放的店铺不会进行挂机战斗。", "查看概览", ManagementSection.Overview);
            return;
        }

        if (store.ServedCustomers == 0)
        {
            Set("挂机进行中", "等待毛毛完成首位顾客", "无需操作；保持游戏运行，毛毛会自动投掷商品。", "查看战斗", ManagementSection.Overview);
            return;
        }

        if (store.DroppedProducts == 0)
        {
            Set("下一步", "等待第一件商品掉落", "成功招待顾客后有机会掉落商品，掉落会自动进入图鉴。", "查看成果", ManagementSection.Overview);
            return;
        }

        if (canOpenNewStore)
        {
            Set("可选决策", "比较下一家店", "新店会独立挂机，利润方式、客流和风险各不相同。", "选择新店", ManagementSection.Investment);
            return;
        }

        Set("可选决策", "调整商品组合", "比较威力、出手间隔与收益倍率；不调整也会持续挂机。", "调整装备", ManagementSection.Strategy);
    }

    private void Set(
        string context,
        string title,
        string detail,
        string action,
        ManagementSection section)
    {
        ContextText = context;
        Title = title;
        DetailText = detail;
        ActionText = action;
        SuggestedSection = section;
    }
}
