using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

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
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        new CannonBallJob
        {
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule(); //multithreat here pls for improved performance
    }
}
[BurstCompile]
public partial struct CannonBallJob : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;

    void Execute(Entity e, ref CannonBalls ball, ref LocalTransform xform)
    {
        xform.Position += ball.Velocity * DeltaTime;

        ball.Lifetime += DeltaTime;
        if (ball.Lifetime >= 5f)
            ECB.DestroyEntity(e);
    }
}
