using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed class CustomerSpawnPoolService
{
    private readonly CustomerArchetypeDefinition[] _customers;
    private readonly CustomerSpawnPoolDefinition[] _pools;
    private readonly CustomerSpawnEventModifierDefinition[] _modifiers;
    private readonly IReadOnlyDictionary<string, CustomerArchetypeDefinition> _customerById;

    public CustomerSpawnPoolService(
        IReadOnlyList<CustomerArchetypeDefinition> customers,
        IReadOnlyList<CustomerSpawnPoolDefinition> pools,
        IReadOnlyList<CustomerSpawnEventModifierDefinition> modifiers)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(pools);
        ArgumentNullException.ThrowIfNull(modifiers);
        if (customers.Count == 0 || pools.Count == 0)
        {
            throw new ArgumentException("Customers and spawn pools are required.");
        }

        _customers = customers.ToArray();
        _pools = pools.ToArray();
        _modifiers = modifiers.ToArray();
        _customerById = _customers.ToDictionary(customer => customer.Id, StringComparer.Ordinal);
        if (_pools.SelectMany(pool => pool.Entries).Any(entry => !_customerById.ContainsKey(entry.CustomerId)))
        {
            throw new ArgumentException("A spawn pool references an unknown customer.", nameof(pools));
        }
    }

    public CustomerArchetypeDefinition Select(
        int localHour,
        IReadOnlyCollection<string> activeEventTags,
        IRandomSource random)
    {
        if (localHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(localHour));
        }

        ArgumentNullException.ThrowIfNull(activeEventTags);
        ArgumentNullException.ThrowIfNull(random);
        var pool = _pools.SingleOrDefault(candidate => ContainsHour(candidate, localHour))
            ?? throw new InvalidOperationException($"No customer pool covers local hour {localHour}.");
        var weights = pool.Entries.ToDictionary(entry => entry.CustomerId, entry => entry.Weight, StringComparer.Ordinal);
        var activeTags = activeEventTags.ToHashSet(StringComparer.Ordinal);

        foreach (var modifier in _modifiers.Where(modifier => activeTags.Contains(modifier.EventTag)))
        {
            foreach (var customer in _customers.Where(customer => customer.Tags.Contains(modifier.CustomerTag, StringComparer.Ordinal)))
            {
                weights.TryGetValue(customer.Id, out var currentWeight);
                var adjusted = checked(((long)currentWeight * modifier.WeightModifierPermille / 1_000) + modifier.AddedWeight);
                if (adjusted > int.MaxValue)
                {
                    throw new InvalidOperationException("Customer spawn weight exceeds the supported range.");
                }

                weights[customer.Id] = (int)adjusted;
            }
        }

        var candidates = _customers
            .Select(customer => (Customer: customer, Weight: weights.GetValueOrDefault(customer.Id)))
            .Where(candidate => candidate.Weight > 0)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException("Active events removed every customer from the spawn pool.");
        }

        var totalWeight = checked(candidates.Sum(candidate => candidate.Weight));
        var roll = random.Next(totalWeight);
        if (roll < 0 || roll >= totalWeight)
        {
            throw new InvalidOperationException("Random source returned an out-of-range customer roll.");
        }

        foreach (var candidate in candidates)
        {
            if (roll < candidate.Weight)
            {
                return candidate.Customer;
            }

            roll -= candidate.Weight;
        }

        throw new InvalidOperationException("Customer spawn selection failed.");
    }

    private static bool ContainsHour(CustomerSpawnPoolDefinition pool, int hour) =>
        pool.StartHourInclusive == pool.EndHourExclusive
            || (pool.StartHourInclusive < pool.EndHourExclusive
                ? hour >= pool.StartHourInclusive && hour < pool.EndHourExclusive
                : hour >= pool.StartHourInclusive || hour < pool.EndHourExclusive);
}
