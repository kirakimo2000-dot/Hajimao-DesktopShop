using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed class BusinessCombatService
{
    private readonly object _gate = new();
    private readonly BusinessGameService _game;
    private readonly CombatContentCatalog _content;
    private readonly IStatefulRandomSource _random;
    private readonly BusinessCombatOptions _options;
    private readonly StoreCombatEngine _engine = new();
    private readonly CustomerSpawnPoolService _spawnPools;
    private readonly ProductDropService _drops;
    private readonly ProductLoadoutService _loadouts = new();
    private readonly CombatEventDirector _eventDirector;
    private readonly Dictionary<string, ProductCombatDefinition> _productById;
    private readonly Dictionary<string, CustomerArchetypeDefinition> _customerById;
    private readonly Dictionary<string, StoreCombatState> _states = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoreProductLoadout> _storeLoadouts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoreTotals> _totals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<CombatEvent>> _lastEvents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<ProductDropRoll>> _lastDropRolls = new(StringComparer.Ordinal);
    private readonly LegacyCombatCompatibilitySaveData _compatibility;
    private IReadOnlyList<string> _activeEventTags = [];
    private ProductCollection _collection;
    private CombatEmployeeRoster _employees;

    public BusinessCombatService(
        BusinessGameService game,
        CombatContentCatalog content,
        IStatefulRandomSource random,
        CombatSaveData? restored = null,
        BusinessCombatOptions? options = null)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _options = options ?? new BusinessCombatOptions();
        _productById = content.Products.ToDictionary(product => product.ProductId, StringComparer.Ordinal);
        _customerById = content.Customers.ToDictionary(customer => customer.Id, StringComparer.Ordinal);
        _spawnPools = new CustomerSpawnPoolService(content.Customers, content.SpawnPools, content.EventModifiers);
        _eventDirector = new CombatEventDirector(content.EventModifiers.Select(modifier => modifier.EventTag));
        _drops = new ProductDropService(content.Products, random);
        if (content.Characters.Count(character => character.Id == "maomao-default") != 1)
        {
            throw new ArgumentException("Combat content requires exactly one maomao-default.", nameof(content));
        }

        if (restored is null)
        {
            _collection = new ProductCollection();
            RegisterStarterProductsIfEmpty();
            _compatibility = new LegacyCombatCompatibilitySaveData([]);
            _employees = CreateStarterEmployeeRoster();
        }
        else
        {
            _random.RestoreState(restored.RandomState);
            _collection = new ProductCollection(restored.Collection.Entries
                .Where(entry => _productById.ContainsKey(entry.ProductId)));
            RegisterStarterProductsIfEmpty();
            _compatibility = restored.Compatibility;
            _employees = RestoreEmployeeRoster(restored.Employees);
            var occupiedProducts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var saved in restored.Loadouts)
            {
                var supportedProducts = saved.ProductIds
                    .Where(productId => _productById.ContainsKey(productId)
                        && _collection.IsUnlocked(productId)
                        && occupiedProducts.Add(productId))
                    .Distinct(StringComparer.Ordinal)
                    .Take(saved.UnlockedSlots)
                    .ToArray();

                _storeLoadouts.TryAdd(
                    saved.StoreId,
                    new StoreProductLoadout(saved.StoreId, saved.UnlockedSlots, supportedProducts));
            }

            foreach (var saved in restored.Stores)
            {
                _states.Add(saved.StoreId, SanitizeRestoredState(saved.State));
                _totals.Add(saved.StoreId, new StoreTotals(
                    saved.RevenueCents,
                    saved.ServedCustomers,
                    saved.EscapedCustomers,
                    saved.DroppedProducts,
                    saved.EncounteredCustomers,
                    saved.TotalDamage));
            }
        }

        EnsureOpenStores();
    }

    private void RegisterStarterProductsIfEmpty()
    {
        if (_collection.Entries.Count != 0)
        {
            return;
        }

        foreach (var product in _content.Products.Take(3))
        {
            _collection.RegisterCopy(product.ProductId);
        }
    }

    public BusinessCombatSnapshot Tick(
        int localHour,
        IReadOnlyCollection<string> activeEventTags)
    {
        lock (_gate)
        {
            ArgumentNullException.ThrowIfNull(activeEventTags);
            _activeEventTags = activeEventTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tag => tag, StringComparer.Ordinal)
                .ToArray();
            EnsureOpenStores();
            var snapshots = new List<StoreCombatSnapshot>();
            var openStores = _game.GetSnapshot().Stores.ToDictionary(store => store.Id, StringComparer.Ordinal);
            foreach (var storeId in OpenStoreIds())
            {
                if (_employees.AssignedTo(storeId).Count == 0)
                {
                    _lastEvents[storeId] = [];
                    _lastDropRolls[storeId] = [];
                    snapshots.Add(ToSnapshot(storeId, [], []));
                    continue;
                }

                var state = _states[storeId];
                var profile = StoreCombatProfilePolicy.Resolve(openStores[storeId].StoreFormatId);
                var activeCustomerCapacity = Math.Max(
                    1,
                    _options.MaxActiveCustomersPerStore * profile.ActiveCustomerCapacityPermille / 1_000);
                var spawnChance = Math.Min(
                    10_000,
                    _options.SpawnChanceBasisPoints * profile.ArrivalModifierPermille / 1_000);
                CustomerSpawnRequest? spawn = null;
                if (state.Customers.Count < activeCustomerCapacity
                    && Roll(spawnChance))
                {
                    var customer = _spawnPools.Select(localHour, activeEventTags, _random);
                    spawn = new CustomerSpawnRequest(
                        customer.Id,
                        Math.Max(1, customer.DemandHp * profile.DemandHpModifierPermille / 1_000),
                        Math.Max(1, customer.MovementPermillePerTick * profile.MovementModifierPermille / 1_000),
                        customer.Tags,
                        customer.ResistancePermille);
                }

                var loadout = _storeLoadouts[storeId];
                var masteryByProduct = _collection.Entries.ToDictionary(
                    entry => entry.ProductId,
                    entry => entry.MasteryLevel,
                    StringComparer.Ordinal);
                var domainProducts = loadout.ProductIds
                    .Select(productId => ToDomain(
                        _productById[productId],
                        masteryByProduct.GetValueOrDefault(productId, 1)))
                    .ToArray();
                var character = _content.Characters.Single(item => item.Id == "maomao-default");
                var tick = _engine.Tick(
                    state,
                    new CharacterCombatStats(character.BaseAttackIntervalTicks, character.ProjectileTravelTicks),
                    domainProducts,
                    spawn);
                _states[storeId] = tick.State;

                var totals = _totals[storeId];
                var dropRolls = new List<ProductDropRoll>();
                totals = totals with
                {
                    EncounteredCustomers = checked(
                        totals.EncounteredCustomers + tick.Events.OfType<CustomerSpawnedEvent>().Count()),
                    TotalDamage = checked(
                        totals.TotalDamage + tick.Events.OfType<ProductHitEvent>().Sum(hit => (long)hit.Damage))
                };
                foreach (var escaped in tick.Events.OfType<CustomerEscapedEvent>())
                {
                    totals = totals with { EscapedCustomers = totals.EscapedCustomers + 1 };
                }

                foreach (var served in tick.Events.OfType<CustomerServedEvent>())
                {
                    var customer = _customerById[served.ArchetypeId];
                    var finalHit = tick.Events
                        .OfType<ProductHitEvent>()
                        .Last(hit => hit.CustomerEntityId == served.CustomerEntityId);
                    var product = _productById[finalHit.ProductId];
                    var masteryLevel = _collection.Entries
                        .Single(entry => entry.ProductId == finalHit.ProductId)
                        .MasteryLevel;
                    var revenue = checked(
                        customer.BaseRewardCents
                        * product.RevenueModifierPermille
                        / 1_000
                        * profile.RewardModifierPermille
                        / 1_000
                        * ProductMasteryScaling.RevenuePermille(masteryLevel)
                        / 1_000);
                    _game.RecordCombatServiceRevenue(storeId, revenue);
                    var bonusDropPermille = product.Effect == ProductEffectKind.BonusDrop
                        ? product.EffectStrengthPermille
                        : 0;
                    var drop = EnsureFirstServiceUnlock(
                        customer,
                        _drops.Roll(customer, bonusDropPermille),
                        totals.ServedCustomers == 0);
                    var newlyUnlockedProductIds = drop.ProductIds
                        .Where(productId => !_collection.IsUnlocked(productId))
                        .ToHashSet(StringComparer.Ordinal);
                    foreach (var productId in drop.ProductIds)
                    {
                        _collection.RegisterCopy(productId);
                    }

                    dropRolls.AddRange(drop.Rolls.Select(roll => roll with
                    {
                        IsNewUnlock = roll.Awarded
                            && roll.ProductId is not null
                            && newlyUnlockedProductIds.Remove(roll.ProductId)
                    }));
                    totals = totals with
                    {
                        RevenueCents = checked(totals.RevenueCents + revenue),
                        ServedCustomers = totals.ServedCustomers + 1,
                        DroppedProducts = totals.DroppedProducts + drop.ProductIds.Count
                    };
                }

                _totals[storeId] = totals;
                var unlockedSlots = ProductSlotProgressionPolicy.SlotsForServedCustomers(totals.ServedCustomers);
                if (unlockedSlots > _storeLoadouts[storeId].UnlockedSlots)
                {
                    var current = _storeLoadouts[storeId];
                    _storeLoadouts[storeId] = new StoreProductLoadout(
                        storeId,
                        unlockedSlots,
                        current.ProductIds);
                }
                _lastEvents[storeId] = tick.Events;
                _lastDropRolls[storeId] = dropRolls.ToArray();
                snapshots.Add(ToSnapshot(storeId, tick.Events, dropRolls));
            }

            return CreateSnapshot(snapshots);
        }
    }

    public BusinessCombatSnapshot Tick(int localHour)
    {
        lock (_gate)
        {
            return Tick(localHour, _eventDirector.Tick(localHour));
        }
    }

    public BusinessCombatSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            EnsureOpenStores();
            return CreateSnapshot(OpenStoreIds()
                .Select(storeId => ToSnapshot(storeId, _lastEvents[storeId], _lastDropRolls[storeId]))
                .ToArray());
        }
    }

    public string GetInteriorBackgroundAssetPath(string storeId)
    {
        var store = _game.GetSnapshot().Stores.Single(item => item.Id == storeId);
        return _content.Interiors
            .FirstOrDefault(interior => interior.StoreId == store.StoreBrandId)
            ?.BackgroundAssetPath
            ?? _content.Interiors.First().BackgroundAssetPath;
    }

    public IReadOnlyDictionary<string, string> GetProductIconKeys(string storeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);
        return _game.GetProductDefinitions()
            .Where(product => _productById.ContainsKey(product.Id))
            .ToDictionary(product => product.Id, product => product.IconKey, StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> GetProductDisplayNames() =>
        _game.GetProductDefinitions()
            .Where(product => _productById.ContainsKey(product.Id))
            .ToDictionary(product => product.Id, product => product.Name, StringComparer.Ordinal);

    public StoreProductLoadout Equip(string storeId, int slotIndex, string productId)
    {
        lock (_gate)
        {
            EnsureOpenStores();
            var updated = _loadouts.Equip(_storeLoadouts[storeId], _collection, slotIndex, productId);
            TransferProductFromOtherStore(storeId, productId);
            _storeLoadouts[storeId] = updated;
            return updated;
        }
    }

    public StoreProductLoadout ReplaceLoadout(string storeId, IReadOnlyList<string> productIds)
    {
        lock (_gate)
        {
            EnsureOpenStores();
            ArgumentNullException.ThrowIfNull(productIds);
            if (productIds.Any(productId => !_collection.IsUnlocked(productId)))
            {
                throw new InvalidOperationException("Every recommended product must be unlocked.");
            }

            var current = _storeLoadouts[storeId];
            var updated = new StoreProductLoadout(storeId, current.UnlockedSlots, productIds);
            foreach (var productId in productIds)
            {
                TransferProductFromOtherStore(storeId, productId);
            }
            _storeLoadouts[storeId] = updated;
            return updated;
        }
    }

    public IReadOnlyList<ProductCombatDefinition> GetProductDefinitions() => _content.Products;

    public CombatEmployeeSnapshot AssignEmployee(string employeeInstanceId, string storeId)
    {
        lock (_gate)
        {
            EnsureOpenStores();
            if (!_storeLoadouts.ContainsKey(storeId))
            {
                throw new KeyNotFoundException($"Store '{storeId}' is not open.");
            }

            _employees = _employees.Assign(employeeInstanceId, storeId);
            var employee = _employees.Employees.Single(item => item.InstanceId == employeeInstanceId);
            return ToSnapshot(employee);
        }
    }

    public CombatSaveData CaptureSaveData()
    {
        lock (_gate)
        {
            EnsureOpenStores();
            return new CombatSaveData(
                new ProductCollectionSaveData(_collection.Entries),
                OpenStoreIds()
                    .Select(storeId => new StoreProductLoadoutSaveData(
                        storeId,
                        _storeLoadouts[storeId].UnlockedSlots,
                        _storeLoadouts[storeId].ProductIds.ToArray()))
                    .ToArray(),
                OpenStoreIds()
                    .Select(storeId =>
                    {
                        var totals = _totals[storeId];
                        return new StoreCombatStateSaveData(
                            storeId,
                            _states[storeId],
                            totals.RevenueCents,
                            totals.ServedCustomers,
                            totals.EscapedCustomers,
                            totals.DroppedProducts,
                            totals.EncounteredCustomers,
                            totals.TotalDamage);
                    })
                    .ToArray(),
                _random.State,
                _compatibility,
                _employees.Employees.Select(item => new CombatEmployeeSaveData(
                    item.InstanceId,
                    item.CharacterId,
                    item.DisplayName,
                    item.AssignedStoreId)).ToArray());
        }
    }

    private StoreCombatState SanitizeRestoredState(StoreCombatState state)
    {
        var customers = state.Customers
            .Where(customer => _customerById.ContainsKey(customer.ArchetypeId))
            .ToArray();
        var customerIds = customers
            .Select(customer => customer.EntityId)
            .ToHashSet();
        var projectiles = state.Projectiles
            .Where(projectile => _productById.ContainsKey(projectile.ProductId)
                && customerIds.Contains(projectile.TargetCustomerEntityId))
            .ToArray();
        return state with
        {
            Customers = customers,
            Projectiles = projectiles
        };
    }

    private void EnsureOpenStores()
    {
        var starterProducts = _collection.Entries
            .Select(entry => entry.ProductId)
            .Where(_productById.ContainsKey)
            .Take(3)
            .ToArray();
        var openStoreIds = OpenStoreIds();
        for (var index = 0; index < openStoreIds.Length; index++)
        {
            var storeId = openStoreIds[index];
            _states.TryAdd(storeId, StoreCombatState.Empty);
            _totals.TryAdd(storeId, new StoreTotals(0, 0, 0, 0, 0, 0));
            _storeLoadouts.TryAdd(
                storeId,
                new StoreProductLoadout(
                    storeId,
                    3,
                    index == 0 && _storeLoadouts.Count == 0 ? starterProducts : []));
            _lastEvents.TryAdd(storeId, []);
            _lastDropRolls.TryAdd(storeId, []);
        }


        while (_employees.Employees.Count < openStoreIds.Length)
        {
            var sequence = _employees.Employees.Count + 1;
            var instanceId = $"maomao-{sequence:0000}";
            while (_employees.Employees.Any(item => string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal)))
            {
                sequence++;
                instanceId = $"maomao-{sequence:0000}";
            }

            var targetStoreId = openStoreIds.FirstOrDefault(storeId => _employees.AssignedTo(storeId).Count == 0);
            _employees = _employees.Add(new CombatEmployeeInstance(
                instanceId,
                "maomao-default",
                $"毛毛 {sequence}",
                targetStoreId));
        }
    }

    private string[] OpenStoreIds() =>
        _game.GetSnapshot().Stores
            .OrderBy(store => store.StreetOrdinal)
            .ThenBy(store => store.Id, StringComparer.Ordinal)
            .Select(store => store.Id)
            .ToArray();

    private CombatEmployeeRoster CreateStarterEmployeeRoster() =>
        new(
        [
            new CombatEmployeeInstance(
                "maomao-0001",
                "maomao-default",
                "毛毛",
                OpenStoreIds().First())
        ]);

    private CombatEmployeeRoster RestoreEmployeeRoster(IReadOnlyList<CombatEmployeeSaveData>? saved)
    {
        var openStores = OpenStoreIds().ToHashSet(StringComparer.Ordinal);
        var supportedCharacters = _content.Characters
            .Select(item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        var occupiedStores = new HashSet<string>(StringComparer.Ordinal);
        var employees = new List<CombatEmployeeInstance>();
        foreach (var item in saved ?? [])
        {
            if (!supportedCharacters.Contains(item.CharacterId)
                || string.IsNullOrWhiteSpace(item.InstanceId)
                || string.IsNullOrWhiteSpace(item.DisplayName)
                || !instanceIds.Add(item.InstanceId))
            {
                continue;
            }

            var assignedStoreId = item.AssignedStoreId is not null
                && openStores.Contains(item.AssignedStoreId)
                && occupiedStores.Add(item.AssignedStoreId)
                    ? item.AssignedStoreId
                    : null;
            employees.Add(new CombatEmployeeInstance(
                item.InstanceId,
                item.CharacterId,
                item.DisplayName,
                assignedStoreId));
        }
        return employees.Count == 0 ? CreateStarterEmployeeRoster() : new CombatEmployeeRoster(employees);
    }

    private void TransferProductFromOtherStore(string targetStoreId, string productId)
    {
        foreach (var entry in _storeLoadouts.ToArray())
        {
            if (string.Equals(entry.Key, targetStoreId, StringComparison.Ordinal)
                || !entry.Value.ProductIds.Contains(productId, StringComparer.Ordinal))
            {
                continue;
            }

            _storeLoadouts[entry.Key] = new StoreProductLoadout(
                entry.Key,
                entry.Value.UnlockedSlots,
                entry.Value.ProductIds.Where(item => !string.Equals(item, productId, StringComparison.Ordinal)).ToArray());
        }
    }

    private bool Roll(int chanceBasisPoints)
    {
        if (chanceBasisPoints == 0)
        {
            return false;
        }

        if (chanceBasisPoints == 10_000)
        {
            return true;
        }

        return _random.Next(10_000) < chanceBasisPoints;
    }

    private ProductDropResult EnsureFirstServiceUnlock(
        CustomerArchetypeDefinition customer,
        ProductDropResult drop,
        bool isFirstService)
    {
        if (!isFirstService
            || drop.ProductIds.Any(productId => !_collection.IsUnlocked(productId)))
        {
            return drop;
        }

        var guaranteedProductId = customer.ProductDropWeights
            .Where(entry => !_collection.IsUnlocked(entry.Key))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Key)
            .FirstOrDefault();
        return guaranteedProductId is null
            ? drop
            : new ProductDropResult(
                [.. drop.ProductIds, guaranteedProductId],
                [.. drop.Rolls, new ProductDropRoll(
                    "first-service-guarantee",
                    0,
                    1,
                    guaranteedProductId,
                    Awarded: true)]);
    }

    private StoreCombatSnapshot ToSnapshot(
        string storeId,
        IReadOnlyList<CombatEvent> events,
        IReadOnlyList<ProductDropRoll> dropRolls)
    {
        var totals = _totals[storeId];
        var store = _game.GetSnapshot().Stores.Single(item => item.Id == storeId);
        var profile = StoreCombatProfilePolicy.Resolve(store.StoreFormatId);
        return new StoreCombatSnapshot(
            storeId,
            _states[storeId],
            events,
            dropRolls,
            totals.RevenueCents,
            totals.ServedCustomers,
            totals.EscapedCustomers,
            totals.DroppedProducts,
            store.StoreFormatId,
            profile,
            totals.EncounteredCustomers,
            totals.TotalDamage,
            _employees.AssignedTo(storeId).Count > 0,
            _employees.AssignedTo(storeId).SingleOrDefault()?.InstanceId);
    }

    private BusinessCombatSnapshot CreateSnapshot(IReadOnlyList<StoreCombatSnapshot> stores) =>
        new(
            _game.GetSnapshot().CashCents,
            stores,
            _collection.Entries,
            OpenStoreIds().Select(storeId => _storeLoadouts[storeId]).ToArray(),
            _activeEventTags,
            _employees.Employees.Select(ToSnapshot).ToArray(),
            _collection.Entries.Select(entry => new ProductAssignmentSnapshot(
                entry.ProductId,
                _storeLoadouts.Values.SingleOrDefault(loadout =>
                    loadout.ProductIds.Contains(entry.ProductId, StringComparer.Ordinal))?.StoreId)).ToArray());

    private static CombatEmployeeSnapshot ToSnapshot(CombatEmployeeInstance employee) =>
        new(employee.InstanceId, employee.CharacterId, employee.DisplayName, employee.AssignedStoreId);

    private static ProductCombatStats ToDomain(ProductCombatDefinition product, int masteryLevel) =>
        new(
            product.ProductId,
            Math.Max(1, product.BasePower * ProductMasteryScaling.PowerPermille(masteryLevel) / 1_000),
            product.AttackIntervalTicks,
            product.Tags,
            product.Effect switch
            {
                ProductEffectKind.None => ProductCombatEffectKind.None,
                ProductEffectKind.Splash => ProductCombatEffectKind.Splash,
                ProductEffectKind.Slow => ProductCombatEffectKind.Slow,
                ProductEffectKind.BonusDrop => ProductCombatEffectKind.BonusDrop,
                _ => throw new ArgumentOutOfRangeException(nameof(product))
            },
            product.EffectStrengthPermille);

    private sealed record StoreTotals(
        long RevenueCents,
        int ServedCustomers,
        int EscapedCustomers,
        int DroppedProducts,
        int EncounteredCustomers,
        long TotalDamage);
}
