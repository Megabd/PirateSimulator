using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

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

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        if (config.ScheduleParallel)
        {
            var job = new CannonBallTriggerEventJob //parallel version not implemented yet
            {
                BallLookup = _ballLookup,
                ShipLookup = _shipLookup,
                HealthLookup = _healthLookup,   
                ECB = ecb,
                Damage = 1
            };
            //state.Dependency = job.Schedule(sim, state.Dependency);
            ecb.Dispose();

        }
        else if (config.Schedule)
        {
            var job = new CannonBallTriggerEventJob // scheduled version
            {
                BallLookup = _ballLookup,
                ShipLookup = _shipLookup,
                HealthLookup = _healthLookup,
                ECB = ecb,
                Damage = 1
            };
            var handle = job.Schedule(sim, state.Dependency);

            handle.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Dependency = handle;

        }
        else
        {
            // TODO: main-thread version – we'll come back to this
            //job.Run(sim);
            ecb.Dispose();
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

        ECB.AddComponent(ball, new PendingDestroyTag());
    }

}

public struct PendingDestroyTag : IComponentData { }

