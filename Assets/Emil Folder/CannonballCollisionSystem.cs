using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CannonBallSystem))] // after movement
public partial struct CannonBallCollisionSystem : ISystem
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
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var balls = new NativeList<BallData>(Allocator.TempJob);

        foreach (var (ball, xform, entity)
                 in SystemAPI.Query<RefRO<CannonBalls>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            // Ignore brand-new balls to avoid instant self-collisions at spawn
            if (ball.ValueRO.Lifetime < 0.05f)
                continue;

            balls.Add(new BallData
            {
                Entity = entity,
                Position = xform.ValueRO.Position,
                Radius = ball.ValueRO.Radius
            });
        }

        if (balls.Length <= 1)
        {
            balls.Dispose();
            return;
        }

        var toDestroy = new NativeParallelHashSet<Entity>(balls.Length * 2, Allocator.Temp);

        for (int i = 0; i < balls.Length; i++)
        {
            var a = balls[i];
            for (int j = i + 1; j < balls.Length; j++)
            {
                var b = balls[j];

                float sumRadius = a.Radius + b.Radius;
                float3 diff = a.Position - b.Position;
                float distSq = math.lengthsq(diff);

                if (distSq <= sumRadius * sumRadius)
                {
                    toDestroy.Add(a.Entity);
                    toDestroy.Add(b.Entity);
                }
            }
        }

        foreach (var ent in toDestroy)
            ecb.DestroyEntity(ent);

        toDestroy.Dispose();
        balls.Dispose();
    }

    private struct BallData
    {
        public Entity Entity;
        public float3 Position;
        public float Radius;
    }
}