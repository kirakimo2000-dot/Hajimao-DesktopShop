using System.Text.Json;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Domain.Tests.Combat;

public sealed class StoreCombatEngineTests
{
    private static readonly CharacterCombatStats Maomao = new(30, 2);
    private static readonly ProductCombatStats Water = new(
        "water", 100, 4, ["liquid"], ProductCombatEffectKind.None, 0);

    [Fact]
    public void Tick_TargetsCustomerNearestTheServiceBoundary()
    {
        var state = State(
            Customer(1, "far", hp: 200, position: 8_000),
            Customer(2, "near", hp: 200, position: 2_000));

        var result = new StoreCombatEngine().Tick(state, Maomao, [Water], spawn: null);

        var thrown = Assert.IsType<ProductThrownEvent>(Assert.Single(result.Events));
        Assert.Equal(2, thrown.TargetCustomerEntityId);
    }

    [Fact]
    public void Tick_RespectsAttackCooldown()
    {
        var engine = new StoreCombatEngine();
        var first = engine.Tick(State(Customer(1, "regular", 500, 8_000)), Maomao, [Water], null);

        var second = engine.Tick(first.State, Maomao, [Water], null);

        Assert.DoesNotContain(second.Events, combatEvent => combatEvent is ProductThrownEvent);
        Assert.Equal(3, second.State.AttackCooldownTicks);
    }

    [Fact]
    public void Tick_ProjectileTravelsBeforeImpact()
    {
        var engine = new StoreCombatEngine();
        var first = engine.Tick(State(Customer(1, "regular", 500, 8_000)), Maomao, [Water], null);
        var second = engine.Tick(first.State, Maomao, [Water], null);
        var third = engine.Tick(second.State, Maomao, [Water], null);

        Assert.DoesNotContain(first.Events, combatEvent => combatEvent is ProductHitEvent);
        Assert.DoesNotContain(second.Events, combatEvent => combatEvent is ProductHitEvent);
        Assert.Contains(third.Events, combatEvent => combatEvent is ProductHitEvent { Damage: 100 });
    }

    [Fact]
    public void Tick_AppliesStrongestMatchingResistance()
    {
        var target = Customer(
            1,
            "resistant",
            hp: 500,
            position: 8_000,
            resistance: new Dictionary<string, int> { ["liquid"] = 300, ["basic"] = 100 });
        var projectile = Projectile(2, target.EntityId, Water with { Tags = ["liquid", "basic"] }, travel: 1);
        var state = new StoreCombatState(3, 10, 0, [target], [projectile]);

        var result = new StoreCombatEngine().Tick(state, Maomao, [Water], null);

        Assert.Contains(result.Events, combatEvent => combatEvent is ProductHitEvent { Damage: 70, RemainingDemandHp: 430 });
    }

    [Fact]
    public void Tick_SplashDamagesOtherActiveCustomers()
    {
        var product = Water with { Effect = ProductCombatEffectKind.Splash, EffectStrengthPermille = 500 };
        var target = Customer(1, "target", 500, 8_000);
        var nearby = Customer(2, "nearby", 500, 9_000);
        var state = new StoreCombatState(4, 10, 0, [target, nearby], [Projectile(3, target.EntityId, product, 1)]);

        var result = new StoreCombatEngine().Tick(state, Maomao, [product], null);

        Assert.Contains(result.Events, combatEvent => combatEvent is ProductHitEvent { CustomerEntityId: 1, Damage: 100 });
        Assert.Contains(result.Events, combatEvent => combatEvent is ProductHitEvent { CustomerEntityId: 2, Damage: 50 });
    }

    [Fact]
    public void Tick_RemovesServedCustomerAndEmitsServiceEvent()
    {
        var target = Customer(1, "quick", 60, 8_000);
        var state = new StoreCombatState(3, 10, 0, [target], [Projectile(2, target.EntityId, Water, 1)]);

        var result = new StoreCombatEngine().Tick(state, Maomao, [Water], null);

        Assert.Empty(result.State.Customers);
        Assert.Contains(result.Events, combatEvent => combatEvent is CustomerServedEvent { CustomerEntityId: 1, ArchetypeId: "quick" });
    }

    [Fact]
    public void Tick_RemovesCustomerThatReachesServiceBoundary()
    {
        var state = State(Customer(1, "escaping", 500, position: 20, movement: 25));

        var result = new StoreCombatEngine().Tick(state, Maomao, [], null);

        Assert.Empty(result.State.Customers);
        Assert.Contains(result.Events, combatEvent => combatEvent is CustomerEscapedEvent { CustomerEntityId: 1 });
    }

    [Fact]
    public void Tick_EmptyLoadoutNeverThrowsAProduct()
    {
        var result = new StoreCombatEngine().Tick(
            State(Customer(1, "regular", 500, 8_000)),
            Maomao,
            [],
            null);

        Assert.Empty(result.State.Projectiles);
        Assert.DoesNotContain(result.Events, combatEvent => combatEvent is ProductThrownEvent);
    }

    [Fact]
    public void Tick_SpawnCreatesCustomerAtStreetEdge()
    {
        var spawn = new CustomerSpawnRequest(
            "student", 120, 80, ["student"], new Dictionary<string, int>());

        var result = new StoreCombatEngine().Tick(StoreCombatState.Empty, Maomao, [], spawn);

        var customer = Assert.Single(result.State.Customers);
        Assert.Equal(10_000, customer.PositionPermille);
        Assert.Contains(result.Events, combatEvent => combatEvent is CustomerSpawnedEvent { ArchetypeId: "student" });
    }

    [Fact]
    public void Tick_IdenticalInputsProduceIdenticalSerializedState()
    {
        var engine = new StoreCombatEngine();
        var initial = State(Customer(1, "regular", 500, 8_000));

        var left = engine.Tick(initial, Maomao, [Water], null);
        var right = engine.Tick(initial, Maomao, [Water], null);

        Assert.Equal(JsonSerializer.Serialize(left), JsonSerializer.Serialize(right));
    }

    private static StoreCombatState State(params ActiveCustomerState[] customers) =>
        new(customers.Length + 1, 0, 0, customers, []);

    private static ActiveCustomerState Customer(
        long id,
        string archetypeId,
        int hp,
        int position,
        int movement = 10,
        IReadOnlyDictionary<string, int>? resistance = null) =>
        new(id, archetypeId, hp, position, movement, [archetypeId], resistance ?? new Dictionary<string, int>(), 0, 0);

    private static ProductProjectileState Projectile(
        long id,
        long targetId,
        ProductCombatStats product,
        int travel) =>
        new(id, product.ProductId, targetId, travel, product.BasePower, product.Tags, product.Effect, product.EffectStrengthPermille);
}
