using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class AutosaveCoordinatorTests
{
    [Fact]
    public async Task TryAutosaveAsync_CoalescesOverlap_AndFlushCapturesLatestState()
    {
        var store = new BlockingSaveStore();
        var captureNumber = 0;
        var coordinator = new AutosaveCoordinator(
            store,
            () => CreateSave(++captureNumber),
            () => new DesktopWindowPlacement(100d + captureNumber, 200d, IsLocked: true));

        var firstSave = coordinator.TryAutosaveAsync();
        await store.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var overlappingResult = await coordinator.TryAutosaveAsync();
        store.ReleaseFirstSave.SetResult();

        Assert.True(await firstSave);
        Assert.False(overlappingResult);
        Assert.Single(store.GameSaves);

        await coordinator.FlushAsync();

        Assert.Equal(2, store.GameSaves.Count);
        Assert.Equal(2, store.GameSaves[^1].Simulation.GameMinute);
        Assert.Equal(new DesktopWindowPlacement(102d, 200d, IsLocked: true), store.Placements[^1]);
    }

    private static GameSaveData CreateSave(long minute) =>
        new(
            GameSaveSchema.CurrentVersion,
            DateTimeOffset.UtcNow,
            new ShopSaveData(50_000, 0, 0, 0, []),
            new SimulationSaveData(
                minute,
                minute,
                1,
                0,
                [],
                [],
                null,
                [],
                null,
                null));

    private sealed class BlockingSaveStore : IGameSaveStore
    {
        public TaskCompletionSource FirstSaveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstSave { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<GameSaveData> GameSaves { get; } = [];

        public List<DesktopWindowPlacement> Placements { get; } = [];

        public Task<GameSaveData?> LoadGameAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<GameSaveData?>(null);

        public async Task SaveGameAsync(GameSaveData save, CancellationToken cancellationToken = default)
        {
            if (GameSaves.Count == 0)
            {
                FirstSaveStarted.TrySetResult();
                await ReleaseFirstSave.Task.WaitAsync(cancellationToken);
            }

            GameSaves.Add(save);
        }

        public Task<DesktopWindowPlacement?> LoadDesktopWindowPlacementAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DesktopWindowPlacement?>(null);

        public Task SaveDesktopWindowPlacementAsync(
            DesktopWindowPlacement placement,
            CancellationToken cancellationToken = default)
        {
            Placements.Add(placement);
            return Task.CompletedTask;
        }
    }
}
