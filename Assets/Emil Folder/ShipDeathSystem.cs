using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct ShipDeathSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HealthComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel){
            new ShipDeathJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new ShipDeathJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.Schedule(); // you can upgrade to ScheduleParallel later
        }
        else
        {
            foreach (var (health, entity)
                    in SystemAPI.Query<RefRO<HealthComponent>>().WithEntityAccess())
            {
                if (health.ValueRO.health <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}


[BurstCompile]
public partial struct ShipDeathJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity e, ref HealthComponent health)
    {
        if (health.health <= 0)
            {
                ECB.DestroyEntity(entityInQueryIndex, e);
            }
    }
}
