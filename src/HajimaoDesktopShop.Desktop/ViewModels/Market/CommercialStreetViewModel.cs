using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;
using HajimaoDesktopShop.Rendering;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class CommercialStreetViewModel : ObservableObject
{
    private string _weatherText = string.Empty;
    private string _tierText = string.Empty;
    private string _trafficText = string.Empty;
    private string _activityText = string.Empty;
    private IReadOnlyList<CommercialStreetStoreItemViewModel> _stores = [];
    private CommercialStreetSceneFrame? _sceneFrame;

    public string WeatherText
    {
        get => _weatherText;
        private set => SetProperty(ref _weatherText, value);
    }

    public string TierText
    {
        get => _tierText;
        private set => SetProperty(ref _tierText, value);
    }

    public string TrafficText
    {
        get => _trafficText;
        private set => SetProperty(ref _trafficText, value);
    }

    public string ActivityText
    {
        get => _activityText;
        private set => SetProperty(ref _activityText, value);
    }

    public IReadOnlyList<CommercialStreetStoreItemViewModel> Stores
    {
        get => _stores;
        private set => SetProperty(ref _stores, value);
    }

    public CommercialStreetSceneFrame? SceneFrame
    {
        get => _sceneFrame;
        private set => SetProperty(ref _sceneFrame, value);
    }

    public void Refresh(
        CommercialStreetSnapshot snapshot,
        int animationFrame,
        bool reduceMotion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        WeatherText = snapshot.Weather switch
        {
            StreetWeather.Clear => "晴朗",
            StreetWeather.Cloudy => "多云",
            StreetWeather.Rain => "雨天",
            StreetWeather.Wind => "大风",
            _ => "未知"
        };
        TierText = snapshot.Tier switch
        {
            CommercialStreetTier.Corner => "街角小店",
            CommercialStreetTier.Neighbors => "相邻店铺",
            CommercialStreetTier.Street => "一段商业街",
            CommercialStreetTier.Block => "完整街区",
            _ => "街角小店"
        };
        TrafficText = string.Format(
            CultureInfo.InvariantCulture,
            "共享客流 {0:0.00}%",
            snapshot.SharedTrafficBasisPoints / 100m);
        ActivityText = $"已开店 {snapshot.Stores.Count} · 路人 {snapshot.VisiblePedestrians} · 车辆 {snapshot.VisibleVehicles}";
        Stores = Array.AsReadOnly(snapshot.Stores
            .Select(store => new CommercialStreetStoreItemViewModel(
                store.StoreName,
                $"吸引力 {store.AttractionBasisPoints / 100m:0.00}%",
                $"客流份额 {store.TrafficShareBasisPoints / 100m:0.00}%"))
            .ToArray());
        SceneFrame = new CommercialStreetSceneFrame(
            snapshot,
            reduceMotion ? 0 : animationFrame,
            reduceMotion);
    }
}

public sealed record CommercialStreetStoreItemViewModel(
    string Name,
    string AttractionText,
    string TrafficShareText);
