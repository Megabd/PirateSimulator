using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.VisualScripting;
using UnityEngine;

// Runs during physics so we see collision events
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
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
        state.RequireForUpdate<Config>(); // use your global config

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
        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel)
        {
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var parallelJob = new CannonBallTriggerEventParallelJob
            {
                BallLookup = _ballLookup,
                ShipLookup = _shipLookup,
                HealthLookup = _healthLookup,
                ECB = ecb,
                Damage = 1
            };

            var handle = parallelJob.Schedule(sim, state.Dependency);
            state.Dependency = handle;
        }
        else if (config.Schedule)
        {
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var job = new CannonBallTriggerEventJob // scheduled version
            {
                BallLookup = _ballLookup,
                ShipLookup = _shipLookup,
                HealthLookup = _healthLookup,
                ECB = ecb,
                Damage = 1
            };
            var handle = job.Schedule(sim, state.Dependency);
            state.Dependency = handle;
        }
        else
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new CannonBallTriggerEventJob
            {
                BallLookup = _ballLookup,
                ShipLookup = _shipLookup,
                HealthLookup = _healthLookup,
                ECB = ecb,
                Damage = 1
            };

            var handle = job.Schedule(sim, state.Dependency);
            handle.Complete();
            ecb.Playback(state.EntityManager); // forced main-thread version i think
            ecb.Dispose();
            state.Dependency = handle;

        }
    }   

    [BurstCompile]
    public void OnDestroy(ref SystemState state) 
    { 

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

        ECB.AddComponent<PendingDestroyTag>(ball);
        //destroy the ball
        //ECB.DestroyEntity(ball);

    }

}

[BurstCompile]
public struct CannonBallTriggerEventParallelJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<CannonBalls> BallLookup;
    public ComponentLookup<ShipAuthoring.Ship> ShipLookup;
    public ComponentLookup<HealthComponent> HealthLookup;

    public EntityCommandBuffer.ParallelWriter ECB;
    public int Damage;

    public void Execute(TriggerEvent triggerEvent)
    {
        var a = triggerEvent.EntityA;
        var b = triggerEvent.EntityB;

        bool aIsBall = BallLookup.HasComponent(a);
        bool bIsBall = BallLookup.HasComponent(b);

        if (!aIsBall && !bIsBall)
            return;

        if (aIsBall && ShipLookup.HasComponent(b))
            HitShip(0, ball: a, ship: b);

        else if (bIsBall && ShipLookup.HasComponent(a))
            HitShip(0, ball: b, ship: a);
    }

    void HitShip(int sortKey, Entity ball, Entity ship)
    {
        var health = HealthLookup[ship];
        health.health -= Damage;
        HealthLookup[ship] = health;

        ECB.AddComponent<PendingDestroyTag>(sortKey, ball);
    }
}

public struct PendingDestroyTag : IComponentData
{
    public bool destroy;
}

