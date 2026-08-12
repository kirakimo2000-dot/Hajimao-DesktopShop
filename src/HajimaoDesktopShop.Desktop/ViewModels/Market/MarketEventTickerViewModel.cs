using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Events;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketEventTickerViewModel : ObservableObject
{
    private string _text = string.Empty;
    private bool _isVisible;

    public string Text
    {
        get => _text;
        private set => SetProperty(ref _text, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public void Update(MarketEventSchedulerSnapshot? snapshot)
    {
        var active = snapshot?.ActiveEvents
            .OrderBy(item => item.RemainingMinutes)
            .ThenBy(item => item.DefinitionId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (active is null)
        {
            Text = string.Empty;
            IsVisible = false;
            return;
        }

        Text = $"{active.Headline} · {active.EffectSummary}（剩余 {FormatDuration(active.RemainingMinutes)}）";
        IsVisible = true;
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} 分钟";
        }

        if (minutes < 1_440)
        {
            return $"{(minutes + 59) / 60} 小时";
        }

        return $"{(minutes + 1_439) / 1_440} 天";
    }
}
