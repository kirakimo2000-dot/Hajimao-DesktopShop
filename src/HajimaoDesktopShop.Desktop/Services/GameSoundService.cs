using System.Media;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Services;

public interface IGameSoundOutput
{
    void Play(GameFeedbackKind kind);
}

public sealed class SystemGameSoundOutput : IGameSoundOutput
{
    public void Play(GameFeedbackKind kind)
    {
        var sound = kind switch
        {
            GameFeedbackKind.RestockQueued => SystemSounds.Asterisk,
            GameFeedbackKind.PriceChanged => SystemSounds.Beep,
            GameFeedbackKind.SaleCompleted => SystemSounds.Exclamation,
            GameFeedbackKind.ProcurementOrdered => SystemSounds.Asterisk,
            GameFeedbackKind.PromotionStarted => SystemSounds.Exclamation,
            _ => SystemSounds.Beep
        };
        sound.Play();
    }
}

public sealed class GameSoundService : IDisposable
{
    private readonly IGameSoundOutput _output;
    private readonly Func<bool> _isMuted;
    private readonly Action _unsubscribe;

    public GameSoundService(GameViewModel viewModel, IGameSoundOutput output)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
        _isMuted = () => viewModel.IsMuted;
        viewModel.FeedbackRaised += OnFeedbackRaised;
        _unsubscribe = () => viewModel.FeedbackRaised -= OnFeedbackRaised;
    }

    public GameSoundService(MarketViewModel viewModel, IGameSoundOutput output)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
        _isMuted = () => viewModel.IsMuted;
        viewModel.FeedbackRaised += OnFeedbackRaised;
        _unsubscribe = () => viewModel.FeedbackRaised -= OnFeedbackRaised;
    }

    public void Dispose() => _unsubscribe();

    private void OnFeedbackRaised(object? sender, GameFeedbackEventArgs e)
    {
        if (!_isMuted())
        {
            _output.Play(e.Kind);
        }
    }
}
