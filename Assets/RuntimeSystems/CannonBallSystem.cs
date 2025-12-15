using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics.Systems;

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

        var job = new CannonBallMoveJob
        {
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            DeltaTime = DeltaTime
        };

        if (config.ScheduleParallel){
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }
        else
        {
            state.Dependency.Complete();
            job.Run();
        }
    }
}
// Moves the cannonball and checks if its time has run out
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
