using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        if (config.ScheduleParallel)
        {
            var job = new DestroyPendingBallsParallelJob
            {
                ECB = ecb.AsParallelWriter()
            };

            var handle = job.ScheduleParallel(state.Dependency);
            handle.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Dependency = handle;
        }
        else if (config.Schedule)
        {
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb
            };

            var handle = job.Schedule(state.Dependency);
            handle.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Dependency = handle;
        }
        else
        {
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb
            };

            job.Run();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

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
