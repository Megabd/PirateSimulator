using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics.Systems;
using System.ComponentModel;
using Unity.Collections;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct CannonBallSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CannonBalls>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var DeltaTime = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel){
            new CannonBallMoveJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new CannonBallMoveJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.Schedule(); // you can upgrade to ScheduleParallel later
        }
        else
        {
            var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (ball, transform, entity)
                    in SystemAPI.Query<RefRW<CannonBalls>, RefRW<LocalTransform>>().WithEntityAccess())
            {
            transform.ValueRW.Position += ball.ValueRO.Velocity * DeltaTime;

            ball.ValueRW.Lifetime -= DeltaTime;
            if (ball.ValueRO.Lifetime <= 0f) ECB.AddComponent<PendingDestroyTag>(entity);
            }
    }
}
}

[BurstCompile]
public partial struct CannonBallMoveJob : IJobEntity
{

    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;

    void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity e, ref CannonBalls ball, ref LocalTransform xform)
    {
        xform.Position += ball.Velocity * DeltaTime;
        ball.Lifetime -= DeltaTime;
        if (ball.Lifetime <= 0f) ECB.AddComponent<PendingDestroyTag>(entityInQueryIndex, e);
    }
}
