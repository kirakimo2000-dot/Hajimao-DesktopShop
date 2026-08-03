using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation.Customers;
using HajimaoDesktopShop.Application.Simulation.Employees;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Simulation;

public sealed class ShopSimulation
{
    private readonly object _gate = new();
    private readonly ShopGameService _game;
    private readonly IRandomSource _random;
    private readonly SimulationClock _clock;
    private readonly double _customerSpawnChance;
    private readonly int _maxCustomers;
    private readonly List<CustomerActor> _customers = [];
    private readonly Queue<long> _checkoutQueue = [];
    private readonly Queue<RestockTask> _restockQueue = [];
    private long _nextCustomerId = 1;
    private long _tick;
    private long? _cashierCustomerId;
    private RestockTask? _activeRestockTask;
    private int _completedSales;
    private string? _lastRestockFailure;

    public ShopSimulation(
        ShopGameService game,
        IRandomSource random,
        double customerSpawnChance = 0.35d,
        int maxCustomers = 6)
        : this(game, random, restoredState: null, customerSpawnChance, maxCustomers, restoreMarker: false)
    {
    }

    public ShopSimulation(
        ShopGameService game,
        IRandomSource random,
        SimulationSaveData restoredState,
        double customerSpawnChance = 0.35d,
        int maxCustomers = 6)
        : this(
            game,
            random,
            restoredState ?? throw new ArgumentNullException(nameof(restoredState)),
            customerSpawnChance,
            maxCustomers,
            restoreMarker: true)
    {
    }

    private ShopSimulation(
        ShopGameService game,
        IRandomSource random,
        SimulationSaveData? restoredState,
        double customerSpawnChance,
        int maxCustomers,
        bool restoreMarker)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(random);

        if (customerSpawnChance is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(customerSpawnChance));
        }

        if (maxCustomers <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCustomers));
        }

        _game = game;
        _random = random;
        _customerSpawnChance = customerSpawnChance;
        _maxCustomers = maxCustomers;
        _clock = restoredState is null
            ? new SimulationClock()
            : new SimulationClock(restoredState.GameMinute);

        if (restoredState is not null)
        {
            RestoreSimulationState(restoredState);
        }
    }

    public void AdvanceRealSecond() => AdvanceRealSeconds(1);

    public void AdvanceRealSeconds(int seconds)
    {
        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        lock (_gate)
        {
            for (var second = 0; second < seconds; second++)
            {
                _clock.AdvanceRealSecond(ProcessTick);
            }
        }
    }

    public void QueueRestock(string productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        lock (_gate)
        {
            _restockQueue.Enqueue(new RestockTask(productId.Trim(), quantity));
        }
    }

    public SimulationSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var customers = _customers
                .Select(customer => new CustomerSnapshot(
                    customer.Id,
                    customer.State,
                    customer.SelectedProductId))
                .ToArray();

            EmployeeSnapshot[] employees =
            [
                new(
                    "cashier-1",
                    "小葵",
                    EmployeeRole.Cashier,
                    _cashierCustomerId.HasValue ? EmployeeState.Working : EmployeeState.Idle,
                    _cashierCustomerId is long customerId ? $"checkout:{customerId}" : null),
                new(
                    "restocker-1",
                    "阿满",
                    EmployeeRole.Restocker,
                    _activeRestockTask is null ? EmployeeState.Idle : EmployeeState.Working,
                    _activeRestockTask is { } restock
                        ? $"restock:{restock.ProductId}:{restock.Quantity}"
                        : null)
            ];

            return new SimulationSnapshot(
                _clock.GameMinute,
                _game.GetSnapshot(),
                Array.AsReadOnly(customers),
                Array.AsReadOnly(employees),
                _checkoutQueue.Count,
                _restockQueue.Count,
                _completedSales,
                _lastRestockFailure);
        }
    }

    public GameSaveData CaptureSaveData(DateTimeOffset? savedAtUtc = null)
    {
        lock (_gate)
        {
            var shop = _game.GetSnapshot();
            return new GameSaveData(
                GameSaveSchema.CurrentVersion,
                savedAtUtc ?? DateTimeOffset.UtcNow,
                new ShopSaveData(
                    shop.CashCents,
                    shop.RevenueCents,
                    shop.StockPurchaseCostCents,
                    shop.GrossProfitCents,
                    shop.Products
                        .Select(product => new ProductSaveData(product.Id, product.SalePriceCents, product.Quantity))
                        .ToArray()),
                new SimulationSaveData(
                    _clock.GameMinute,
                    _tick,
                    _nextCustomerId,
                    _completedSales,
                    _customers
                        .Select(customer => new CustomerSaveData(
                            customer.Id,
                            customer.State,
                            customer.SelectedProductId,
                            customer.LastTransitionTick))
                        .ToArray(),
                    _checkoutQueue.ToArray(),
                    _cashierCustomerId,
                    _restockQueue
                        .Select(task => new RestockTaskSaveData(task.ProductId, task.Quantity))
                        .ToArray(),
                    _activeRestockTask is null
                        ? null
                        : new RestockTaskSaveData(_activeRestockTask.ProductId, _activeRestockTask.Quantity),
                    _lastRestockFailure));
        }
    }

    private void RestoreSimulationState(SimulationSaveData state)
    {
        if (state.Tick < 0 || state.NextCustomerId <= 0 || state.CompletedSales < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        _tick = state.Tick;
        _nextCustomerId = state.NextCustomerId;
        _completedSales = state.CompletedSales;
        _lastRestockFailure = state.LastRestockFailure;
        _cashierCustomerId = state.CashierCustomerId;

        foreach (var customer in state.Customers)
        {
            _customers.Add(new CustomerActor(
                customer.Id,
                customer.State,
                customer.SelectedProductId,
                customer.LastTransitionTick));
        }

        foreach (var customerId in state.CheckoutQueue)
        {
            _checkoutQueue.Enqueue(customerId);
        }

        foreach (var task in state.RestockQueue)
        {
            _restockQueue.Enqueue(new RestockTask(task.ProductId, task.Quantity));
        }

        if (state.ActiveRestockTask is { } activeTask)
        {
            _activeRestockTask = new RestockTask(activeTask.ProductId, activeTask.Quantity);
        }
    }

    private void ProcessTick()
    {
        _tick++;
        ProcessRestocker();
        ProcessCashier();
        ProcessCustomers();
        TrySpawnCustomer();
    }

    private void ProcessRestocker()
    {
        if (_activeRestockTask is { } activeTask)
        {
            var result = _game.PurchaseStock(activeTask.ProductId, activeTask.Quantity);
            _lastRestockFailure = result.Status == StockPurchaseStatus.Success
                ? null
                : $"{activeTask.ProductId}:{result.Status}";
            _activeRestockTask = null;
            return;
        }

        if (_restockQueue.TryDequeue(out var task))
        {
            _activeRestockTask = task;
        }
    }

    private void ProcessCashier()
    {
        if (_cashierCustomerId is long activeCustomerId)
        {
            var customer = FindCustomer(activeCustomerId);
            if (customer?.SelectedProductId is string productId)
            {
                var sale = _game.Sell(productId, 1);
                if (sale.Status == SaleStatus.Success)
                {
                    _completedSales++;
                }

                customer.TransitionTo(CustomerState.Leaving, _tick);
            }

            _cashierCustomerId = null;
            return;
        }

        while (_checkoutQueue.TryDequeue(out var queuedCustomerId))
        {
            var customer = FindCustomer(queuedCustomerId);
            if (customer?.State != CustomerState.Queueing)
            {
                continue;
            }

            customer.TransitionTo(CustomerState.CheckingOut, _tick);
            _cashierCustomerId = customer.Id;
            return;
        }
    }

    private void ProcessCustomers()
    {
        foreach (var customer in _customers.ToArray())
        {
            if (customer.LastTransitionTick == _tick)
            {
                continue;
            }

            switch (customer.State)
            {
                case CustomerState.Entering:
                    customer.TransitionTo(CustomerState.SeekingProduct, _tick);
                    break;
                case CustomerState.SeekingProduct:
                    SelectProductOrLeave(customer);
                    break;
                case CustomerState.Leaving:
                    _customers.Remove(customer);
                    break;
                case CustomerState.Queueing:
                case CustomerState.CheckingOut:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown customer state: {customer.State}.");
            }
        }
    }

    private void SelectProductOrLeave(CustomerActor customer)
    {
        var availableProducts = _game.GetSnapshot().Products
            .Where(product => product.Quantity > 0)
            .ToArray();

        if (availableProducts.Length == 0)
        {
            customer.TransitionTo(CustomerState.Leaving, _tick);
            return;
        }

        var selectedProduct = availableProducts[_random.Next(availableProducts.Length)];
        customer.SelectedProductId = selectedProduct.Id;
        customer.TransitionTo(CustomerState.Queueing, _tick);
        _checkoutQueue.Enqueue(customer.Id);
    }

    private void TrySpawnCustomer()
    {
        if (_customers.Count >= _maxCustomers || _random.NextDouble() >= _customerSpawnChance)
        {
            return;
        }

        _customers.Add(new CustomerActor(_nextCustomerId++, _tick));
    }

    private CustomerActor? FindCustomer(long customerId) =>
        _customers.Find(customer => customer.Id == customerId);

    private sealed class CustomerActor
    {
        public CustomerActor(long id, long spawnedAtTick)
            : this(id, CustomerState.Entering, null, spawnedAtTick)
        {
        }

        public CustomerActor(
            long id,
            CustomerState state,
            string? selectedProductId,
            long lastTransitionTick)
        {
            if (id <= 0 || lastTransitionTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Id = id;
            State = state;
            SelectedProductId = selectedProductId;
            LastTransitionTick = lastTransitionTick;
        }

        public long Id { get; }

        public CustomerState State { get; private set; }

        public string? SelectedProductId { get; set; }

        public long LastTransitionTick { get; private set; }

        public void TransitionTo(CustomerState state, long tick)
        {
            State = state;
            LastTransitionTick = tick;
        }
    }

    private sealed record RestockTask(string ProductId, int Quantity);
}
