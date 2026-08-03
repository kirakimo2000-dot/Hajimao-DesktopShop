using System.Media;
using HajimaoDesktopShop.Desktop.ViewModels;

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
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        sound.Play();
    }
}

public sealed class GameSoundService : IDisposable
{
    private readonly GameViewModel _viewModel;
    private readonly IGameSoundOutput _output;

    public GameSoundService(GameViewModel viewModel, IGameSoundOutput output)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(output);
        _viewModel = viewModel;
        _output = output;
        _viewModel.FeedbackRaised += OnFeedbackRaised;
    }

    public void Dispose() => _viewModel.FeedbackRaised -= OnFeedbackRaised;

    private void OnFeedbackRaised(object? sender, GameFeedbackEventArgs e)
    {
        if (!_viewModel.IsMuted)
        {
            _output.Play(e.Kind);
        }
    }
}
