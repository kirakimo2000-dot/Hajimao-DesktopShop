namespace HajimaoDesktopShop.Domain.Combat;

public sealed record StoreCombatTickResult(
    StoreCombatState State,
    IReadOnlyList<CombatEvent> Events);

public sealed class StoreCombatEngine
{
    private const int SpawnPositionPermille = 10_000;
    private const int NeutralCharacterIntervalTicks = 30;
    private const int SlowDurationTicks = 24;

    public StoreCombatTickResult Tick(
        StoreCombatState state,
        CharacterCombatStats maomao,
        IReadOnlyList<ProductCombatStats> loadout,
        CustomerSpawnRequest? spawn)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(maomao);
        ArgumentNullException.ThrowIfNull(loadout);
        ValidateState(state, maomao, loadout, spawn);

        var events = new List<CombatEvent>();
        var customers = state.Customers.ToList();
        var projectiles = new List<ProductProjectileState>();
        var nextEntityId = state.NextEntityId;
        long? spawnedCustomerId = null;

        if (spawn is not null)
        {
            spawnedCustomerId = nextEntityId++;
            customers.Add(new ActiveCustomerState(
                spawnedCustomerId.Value,
                spawn.ArchetypeId,
                spawn.DemandHp,
                SpawnPositionPermille,
                spawn.MovementPermillePerTick,
                spawn.Tags.ToArray(),
                new Dictionary<string, int>(spawn.ResistancePermille, StringComparer.Ordinal),
                0,
                0,
                spawn.DemandHp));
            events.Add(new CustomerSpawnedEvent(spawnedCustomerId.Value, spawn.ArchetypeId));
        }

        foreach (var projectile in state.Projectiles.OrderBy(item => item.EntityId))
        {
            var remaining = projectile.RemainingTravelTicks - 1;
            if (remaining > 0)
            {
                projectiles.Add(projectile with { RemainingTravelTicks = remaining });
                continue;
            }

            ApplyImpact(projectile, customers, events);
        }

        MoveCustomers(customers, events, spawnedCustomerId);

        var cooldown = Math.Max(0, state.AttackCooldownTicks - 1);
        var nextProductIndex = state.NextProductIndex;
        if (cooldown == 0 && loadout.Count > 0 && customers.Count > 0)
        {
            var productIndex = Math.Abs(nextProductIndex % loadout.Count);
            var product = loadout[productIndex];
            var target = customers
                .OrderBy(customer => customer.PositionPermille)
                .ThenBy(customer => customer.EntityId)
                .First();
            var projectileId = nextEntityId++;
            projectiles.Add(new ProductProjectileState(
                projectileId,
                product.ProductId,
                target.EntityId,
                maomao.ProjectileTravelTicks,
                product.BasePower,
                product.Tags.ToArray(),
                product.Effect,
                product.EffectStrengthPermille,
                maomao.ProjectileTravelTicks));
            events.Add(new ProductThrownEvent(projectileId, product.ProductId, target.EntityId));
            cooldown = Math.Max(
                1,
                checked(product.AttackIntervalTicks * maomao.BaseAttackIntervalTicks) / NeutralCharacterIntervalTicks);
            nextProductIndex = (productIndex + 1) % loadout.Count;
        }

        return new StoreCombatTickResult(
            new StoreCombatState(
                nextEntityId,
                cooldown,
                nextProductIndex,
                customers.ToArray(),
                projectiles.ToArray()),
            events.ToArray());
    }

    private static void ApplyImpact(
        ProductProjectileState projectile,
        List<ActiveCustomerState> customers,
        List<CombatEvent> events)
    {
        var target = customers.SingleOrDefault(customer => customer.EntityId == projectile.TargetCustomerEntityId);
        if (target is null)
        {
            return;
        }

        ApplyDamage(projectile, target.EntityId, projectile.Power, isSplash: false, customers, events);
        if (projectile.Effect == ProductCombatEffectKind.Slow)
        {
            var liveTarget = customers.SingleOrDefault(customer => customer.EntityId == target.EntityId);
            if (liveTarget is not null)
            {
                var index = customers.IndexOf(liveTarget);
                customers[index] = liveTarget with
                {
                    SlowStrengthPermille = Math.Max(liveTarget.SlowStrengthPermille, projectile.EffectStrengthPermille),
                    SlowTicksRemaining = SlowDurationTicks
                };
            }
        }

        if (projectile.Effect != ProductCombatEffectKind.Splash || projectile.EffectStrengthPermille <= 0)
        {
            return;
        }

        var splashPower = Math.Max(1, checked(projectile.Power * projectile.EffectStrengthPermille) / 1_000);
        var otherCustomerIds = customers
            .Where(customer => customer.EntityId != target.EntityId)
            .Select(customer => customer.EntityId)
            .Order()
            .ToArray();
        foreach (var customerId in otherCustomerIds)
        {
            ApplyDamage(projectile, customerId, splashPower, isSplash: true, customers, events);
        }
    }

    private static void ApplyDamage(
        ProductProjectileState projectile,
        long customerId,
        int rawPower,
        bool isSplash,
        List<ActiveCustomerState> customers,
        List<CombatEvent> events)
    {
        var target = customers.SingleOrDefault(customer => customer.EntityId == customerId);
        if (target is null)
        {
            return;
        }

        var resistance = projectile.Tags
            .Select(tag => target.ResistancePermille.GetValueOrDefault(tag))
            .DefaultIfEmpty(0)
            .Max();
        var damage = Math.Max(1, checked(rawPower * (1_000 - resistance)) / 1_000);
        var remainingHp = Math.Max(0, target.DemandHp - damage);
        events.Add(new ProductHitEvent(
            projectile.EntityId,
            projectile.ProductId,
            customerId,
            damage,
            remainingHp,
            isSplash));

        if (remainingHp == 0)
        {
            customers.Remove(target);
            events.Add(new CustomerServedEvent(target.EntityId, target.ArchetypeId));
        }
        else
        {
            var index = customers.IndexOf(target);
            customers[index] = target with { DemandHp = remainingHp };
        }
    }

    private static void MoveCustomers(
        List<ActiveCustomerState> customers,
        List<CombatEvent> events,
        long? spawnedCustomerId)
    {
        foreach (var customer in customers.OrderBy(item => item.EntityId).ToArray())
        {
            if (customer.EntityId == spawnedCustomerId)
            {
                continue;
            }

            var movementModifier = Math.Clamp(1_000 - customer.SlowStrengthPermille, 100, 1_000);
            var movement = Math.Max(1, checked(customer.MovementPermillePerTick * movementModifier) / 1_000);
            var position = customer.PositionPermille - movement;
            if (position <= 0)
            {
                customers.Remove(customer);
                events.Add(new CustomerEscapedEvent(customer.EntityId, customer.ArchetypeId));
                continue;
            }

            var slowTicks = Math.Max(0, customer.SlowTicksRemaining - 1);
            var index = customers.IndexOf(customer);
            customers[index] = customer with
            {
                PositionPermille = position,
                SlowStrengthPermille = slowTicks == 0 ? 0 : customer.SlowStrengthPermille,
                SlowTicksRemaining = slowTicks
            };
        }
    }

    private static void ValidateState(
        StoreCombatState state,
        CharacterCombatStats maomao,
        IReadOnlyList<ProductCombatStats> loadout,
        CustomerSpawnRequest? spawn)
    {
        if (state.NextEntityId <= 0
            || state.AttackCooldownTicks < 0
            || state.NextProductIndex < 0
            || state.Customers is null
            || state.Projectiles is null
            || maomao.BaseAttackIntervalTicks <= 0
            || maomao.ProjectileTravelTicks <= 0
            || loadout.Any(product => string.IsNullOrWhiteSpace(product.ProductId)
                || product.BasePower <= 0
                || product.AttackIntervalTicks <= 0
                || product.EffectStrengthPermille is < 0 or > 900))
        {
            throw new ArgumentException("Combat state or stats are invalid.");
        }

        if (spawn is not null
            && (string.IsNullOrWhiteSpace(spawn.ArchetypeId)
                || spawn.DemandHp <= 0
                || spawn.MovementPermillePerTick <= 0))
        {
            throw new ArgumentException("Customer spawn request is invalid.", nameof(spawn));
        }
    }
}
