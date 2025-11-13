using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[BurstCompile]
public partial struct CannonballCollisionSystem : ISystem
{
    private ComponentLookup<CannonBalls> _cannonballLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        _cannonballLookup = state.GetComponentLookup<CannonBalls>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        _cannonballLookup.Update(ref state);

        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var job = new CannonballTriggerJob
        {
            CannonballLookup = _cannonballLookup,
            Ecb = ecb
        };

        state.Dependency = job.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    struct CannonballTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<CannonBalls> CannonballLookup;
        public EntityCommandBuffer Ecb;

        public void Execute(TriggerEvent ev)
        {
            var a = ev.EntityA;
            var b = ev.EntityB;

            if (CannonballLookup.HasComponent(a))
                Ecb.DestroyEntity(a);

            if (CannonballLookup.HasComponent(b))
                Ecb.DestroyEntity(b);
        }
    }
}
