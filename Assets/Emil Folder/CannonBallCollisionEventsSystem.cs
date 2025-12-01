using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

// Runs during physics so we see collision events
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
public partial struct CannonBallCollisionEventsSystem : ISystem
{
    private ComponentLookup<CannonBalls> _ballLookup;
    private ComponentLookup<ShipAuthoring.Ship> _shipLookup;
    private ComponentLookup<HealthComponent> _healthLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Ensure physics is present and we have cannonballs at all
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<CannonBalls>();

        _ballLookup = state.GetComponentLookup<CannonBalls>(true);
        _shipLookup = state.GetComponentLookup<ShipAuthoring.Ship>(false);
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _ballLookup.Update(ref state);
        _shipLookup.Update(ref state);
        _healthLookup.Update(ref state);

        var sim = SystemAPI.GetSingleton<SimulationSingleton>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        var job = new CannonBallTriggerEventJob
        {
            BallLookup = _ballLookup,
            ShipLookup = _shipLookup,
            HealthLookup = _healthLookup,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            Damage = 1
        };

        state.Dependency = job.Schedule(sim, state.Dependency);
    }
}

[BurstCompile]
public struct CannonBallTriggerEventJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<CannonBalls> BallLookup;
    public ComponentLookup<ShipAuthoring.Ship> ShipLookup;
    public ComponentLookup<HealthComponent> HealthLookup;

    public EntityCommandBuffer ECB;
    public int Damage;

    public void Execute(TriggerEvent triggerEvent)
    {
        var a = triggerEvent.EntityA;
        var b = triggerEvent.EntityB;

        bool aIsBall = BallLookup.HasComponent(a);
        bool bIsBall = BallLookup.HasComponent(b);

        if (!aIsBall && !bIsBall)
            return;

        // Ship hit by ball
        if (aIsBall && ShipLookup.HasComponent(b))
            HitShip(ball: a, ship: b);

        else if (bIsBall && ShipLookup.HasComponent(a))
            HitShip(ball: b, ship: a);
    }

    void HitShip(Entity ball, Entity ship)
    {
        var health = HealthLookup[ship];
        health.health -= Damage;
        HealthLookup[ship] = health;

        ECB.DestroyEntity(ball);
    }
}
