using CommunityToolkit.Mvvm.ComponentModel;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketEventTickerViewModel : ObservableObject
{
    private string _text = string.Empty;
    private bool _isVisible;

    public string Text { get => _text; private set => SetProperty(ref _text, value); }
    public bool IsVisible { get => _isVisible; private set => SetProperty(ref _isVisible, value); }

    public void Update(IReadOnlyCollection<string> activeEventTags)
    {
        ArgumentNullException.ThrowIfNull(activeEventTags);
        var active = activeEventTags.OrderBy(tag => tag, StringComparer.Ordinal).FirstOrDefault();
        if (active is null)
        {
            Text = string.Empty;
            IsVisible = false;
            return;
        }

        Text = Describe(active);
        IsVisible = true;
    }

    private static string Describe(string tag) => tag switch
    {
        "morning-commute" => "通勤高峰 · 上班族顾客更常出现",
        "rainy-evening" => "雨夜客流 · 外卖骑手减少，社区顾客更谨慎",
        "school-holiday" => "学校假期 · 学生顾客明显增多",
        "office-payday" => "发薪日 · 办公族顾客更常出现",
        "budget-week" => "省钱周 · 价格敏感顾客正在集中到店",
        "local-festival" => "本地节庆 · 游客顾客正在增加",
        "night-owls" => "夜猫时段 · 深夜顾客更常出现",
        "senior-club-visit" => "社区活动 · 老年顾客正在集中到店",
        "lost-tour-group" => "迷路旅行团 · 游客顾客突然增加",
        "quiet-weekday" => "平静工作日 · 稀有顾客暂时减少",
        _ => "街区事件 · 顾客构成发生变化"
    };
}
