namespace HajimaoDesktopShop.Application.Persistence;

public interface IGameSaveStore
{
    Task<GameSaveData?> LoadGameAsync(CancellationToken cancellationToken = default);

    Task SaveGameAsync(GameSaveData save, CancellationToken cancellationToken = default);

    Task<DesktopWindowPlacement?> LoadDesktopWindowPlacementAsync(
        CancellationToken cancellationToken = default);

    Task SaveDesktopWindowPlacementAsync(
        DesktopWindowPlacement placement,
        CancellationToken cancellationToken = default);
}

public sealed record DesktopWindowPlacement(double Left, double Top, bool IsLocked);
