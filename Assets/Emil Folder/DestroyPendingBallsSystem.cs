using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        if (config.ScheduleParallel)
        {
            var job = new DestroyPendingBallsParallelJob
            {
                ECB = ecb.AsParallelWriter(),
                config = config
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        else if (config.Schedule)
        {
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb,
                config = config
            };

            state.Dependency = job.Schedule(state.Dependency);
        }
        else
        {
            // Simple main-thread fallback
            var job = new DestroyPendingBallsJob
            {
                ECB = ecb,
                config = config
            };

            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

[BurstCompile]
public partial struct DestroyPendingBallsJob : IJobEntity
{
    public EntityCommandBuffer ECB;

    public Config config;

    void Execute(Entity entity, ref LocalTransform transform, ref PendingDestroyTag tag)
    {
        if (tag.destroy = true)
        {
        var rng = Unity.Mathematics.Random.CreateFromIndex(1337u);
        float2 xz = rng.NextFloat2(
                new float2(-SeaConfig.halfWidth, -SeaConfig.halfHeight),
                new float2(SeaConfig.halfWidth, SeaConfig.halfHeight));

        var pos = new float3(xz.x, -2f, xz.y);
        transform.Position = pos;
        tag.destroy = false;
        }
    }
}

[BurstCompile]
public partial struct DestroyPendingBallsParallelJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public Config config;

    void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref PendingDestroyTag tag)
    {
        if (tag.destroy = true)
        {
        var rng = Unity.Mathematics.Random.CreateFromIndex(1337u);
        float2 xz = rng.NextFloat2(
                new float2(-SeaConfig.halfWidth, -SeaConfig.halfHeight),
                new float2(SeaConfig.halfWidth, SeaConfig.halfHeight));

        var pos = new float3(xz.x, -2f, xz.y);
        transform.Position = pos;
        tag.destroy = false;
        }
    }
}
