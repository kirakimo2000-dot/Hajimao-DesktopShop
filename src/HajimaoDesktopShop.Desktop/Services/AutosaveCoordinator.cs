using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Desktop.Services;

public sealed class AutosaveCoordinator
{
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly IGameSaveStore _store;
    private readonly Func<GameSaveData> _captureGame;
    private readonly Func<DesktopWindowPlacement?> _capturePlacement;

    public AutosaveCoordinator(
        IGameSaveStore store,
        Func<GameSaveData> captureGame,
        Func<DesktopWindowPlacement?> capturePlacement)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(captureGame);
        ArgumentNullException.ThrowIfNull(capturePlacement);
        _store = store;
        _captureGame = captureGame;
        _capturePlacement = capturePlacement;
    }

    public async Task<bool> TryAutosaveAsync(CancellationToken cancellationToken = default)
    {
        if (!await _saveGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            await SaveCurrentStateAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _saveGate.Release();
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private async Task SaveCurrentStateAsync(CancellationToken cancellationToken)
    {
        var game = _captureGame();
        var placement = _capturePlacement();
        await _store.SaveGameAsync(game, cancellationToken).ConfigureAwait(false);
        if (placement is not null)
        {
            await _store.SaveDesktopWindowPlacementAsync(placement, cancellationToken).ConfigureAwait(false);
        }
    }
}
