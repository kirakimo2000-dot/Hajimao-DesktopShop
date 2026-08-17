namespace HajimaoDesktopShop.Desktop.Services;

public sealed class DesktopSingleInstanceCoordinator : IDisposable
{
    private readonly EventWaitHandle _activationEvent;
    private readonly Mutex _instanceMutex;
    private readonly RegisteredWaitHandle? _activationRegistration;
    private bool _disposed;

    public DesktopSingleInstanceCoordinator(string instanceName, Action activationRequested)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException("Instance name is required.", nameof(instanceName));
        }

        ArgumentNullException.ThrowIfNull(activationRequested);
        var namePrefix = $"Local\\HajimaoDesktopShop.{instanceName}";
        _activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            $"{namePrefix}.Activate");
        _instanceMutex = new Mutex(false, $"{namePrefix}.Instance", out var createdNew);
        IsPrimary = createdNew;
        if (IsPrimary)
        {
            _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
                _activationEvent,
                static (state, _) => ((Action)state!).Invoke(),
                activationRequested,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }
    }

    public bool IsPrimary { get; }

    public void SignalPrimary()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _activationEvent.Set();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activationRegistration?.Unregister(null);
        _instanceMutex.Dispose();
        _activationEvent.Dispose();
    }
}
