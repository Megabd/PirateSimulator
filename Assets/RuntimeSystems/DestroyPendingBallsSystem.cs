using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct DestroyPendingBallsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PendingDestroyTag>();
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);


        if (config.ScheduleParallel)
        {
            var job = new DestroyPendingBallsParallelJob
            {
                ECB = ecb.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        else if (config.Schedule)
        {
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb
            };

            state.Dependency = job.Schedule(state.Dependency);
        }
        else
        {
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb
            };

            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

// Destroys cannonballs marked for death, either by lifetime or hitting
[BurstCompile]
public partial struct DestroyPendingBallsJob : IJobEntity
{
    public EntityCommandBuffer ECB;

    void Execute(Entity entity, in PendingDestroyTag tag)
    {
        ECB.DestroyEntity(entity);
    }
}

[BurstCompile]
public partial struct DestroyPendingBallsParallelJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    void Execute([EntityIndexInQuery] int sortKey, Entity entity, in PendingDestroyTag tag)
    {
        ECB.DestroyEntity(sortKey, entity);
    }
}
