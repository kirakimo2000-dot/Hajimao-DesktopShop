namespace HajimaoDesktopShop.Desktop.Services;

public sealed class SimulationLoop : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Action _advanceRealSecond;
    private readonly TimeSpan _interval;
    private readonly Action<Exception>? _reportFailure;
    private CancellationTokenSource? _cancellation;
    private Task? _loopTask;

    public SimulationLoop(
        Action advanceRealSecond,
        TimeSpan? interval = null,
        Action<Exception>? reportFailure = null)
    {
        ArgumentNullException.ThrowIfNull(advanceRealSecond);

        _advanceRealSecond = advanceRealSecond;
        _interval = interval ?? TimeSpan.FromSeconds(1);
        _reportFailure = reportFailure;
        if (_interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_loopTask is not null)
            {
                return;
            }

            _cancellation = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunAsync(_cancellation.Token));
        }
    }

    public async Task StopAsync()
    {
        Task? loopTask;
        lock (_gate)
        {
            _cancellation?.Cancel();
            loopTask = _loopTask;
        }

        if (loopTask is not null)
        {
            await loopTask.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cancellation?.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                _advanceRealSecond();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            try
            {
                _reportFailure?.Invoke(exception);
            }
            catch
            {
                // Diagnostic reporting must not fault shutdown or mask the simulation failure.
            }
        }
    }
}
